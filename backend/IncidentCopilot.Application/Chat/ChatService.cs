using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Application.Observability;
using IncidentCopilot.Application.PendingActions;
using IncidentCopilot.Core.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace IncidentCopilot.Application.Chat;

public sealed class ChatService : IChatService
{
    private const int TopK = 4;
    private const double MaxDistance = 0.75;
    private const int MaxAgentRounds = 5;
    private const string AgentRoundLimitMessage =
        "I couldn't complete the investigation within the maximum of 5 agent rounds.";
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IKnowledgeRetriever _retriever;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<ChatService> _logger;
    private readonly ChatDiagnosticsOptions _diagnosticsOptions;
    private readonly IReadOnlyDictionary<string, IAgentTool> _agentTools;
    private readonly IReadOnlyDictionary<string, IToolApprovalPolicy>
        _approvalPolicies;
    private readonly ToolExecutor _toolExecutor;
    private readonly IPendingActionService _pendingActionService;

    public ChatService(
        IKnowledgeRetriever retriever,
        ILlmClient llmClient,
        IEnumerable<IAgentTool> agentTools,
        IEnumerable<IToolApprovalPolicy> approvalPolicies,
        ToolExecutor toolExecutor,
        IPendingActionService pendingActionService,
        ILogger<ChatService> logger,
        IOptions<ChatDiagnosticsOptions> diagnosticsOptions)
    {
        _retriever = retriever;
        _llmClient = llmClient;
        _toolExecutor = toolExecutor;
        _pendingActionService = pendingActionService;
        _logger = logger;
        _diagnosticsOptions = diagnosticsOptions.Value;
        _agentTools = agentTools.ToDictionary(
            tool => tool.Name,
            StringComparer.Ordinal);
        _approvalPolicies = approvalPolicies.ToDictionary(
            policy => policy.ToolName,
            StringComparer.Ordinal);

        if (_agentTools.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one agent tool must be registered.");
        }
    }

    public Task<ChatStreamResponse> AskStreamAsync(
        string message,
        CancellationToken cancellationToken)
    {
        return PrepareAgentStreamAsync(
            message,
            new List<ToolCallTrace>(),
            cancellationToken);
    }

    public async Task<AgentRunResult> RunAgentAsync(
        string message,
        CancellationToken cancellationToken)
    {
        var toolCalls = new List<ToolCallTrace>();
        var response = await PrepareAgentStreamAsync(
            message,
            toolCalls,
            cancellationToken);
        var content = new StringBuilder();
        PendingActionView? pendingAction = null;

        await foreach (var streamEvent in response.Events
            .WithCancellation(cancellationToken))
        {
            switch (streamEvent)
            {
                case ChatDeltaEvent delta:
                    content.Append(delta.Content);
                    break;
                case ChatPendingActionEvent pending:
                    pendingAction = pending.Action;
                    break;
            }
        }

        return new AgentRunResult(
            content.ToString(),
            toolCalls.ToArray(),
            pendingAction);
    }

    private async Task<ChatStreamResponse> PrepareAgentStreamAsync(
        string message,
        ICollection<ToolCallTrace> toolCalls,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(message));
        }

        _logger.LogInformation(
            "Chat query:{NewLine}{Query}",
            Environment.NewLine,
            message);

        var retrievedChunks = await _retriever.SearchAsync(
            message,
            TopK,
            cancellationToken);

        var relevantChunks = retrievedChunks
            .Where(chunk => chunk.Distance <= MaxDistance)
            .ToList();

        _logger.LogInformation(
            "Retrieved chunks: {RetrievedChunkCount}. Number surviving threshold: {SurvivingChunkCount}. Maximum distance: {MaxDistance:F2}.",
            retrievedChunks.Count,
            relevantChunks.Count,
            MaxDistance);

        LogRetrievedChunks(retrievedChunks);

        var context = BuildContext(relevantChunks);
        var systemPrompt = BuildSystemPrompt(context);

        // Sources come from retrieved context, never from the LLM response.
        var sources = relevantChunks
            .DistinctBy(chunk => (chunk.FileName, chunk.Section))
            .Select(chunk => new ChatSource(
                chunk.ChunkId,
                chunk.FileName,
                chunk.Section))
            .ToList();

        if (relevantChunks.Count > 0)
        {
            _logger.LogInformation(
                "RAG context selected: {RelevantChunkCount} chunk(s).",
                relevantChunks.Count);
        }
        else
        {
            _logger.LogInformation(
                "RAG context selected: none. The response must use a tool or return the scope refusal.");
        }

        return new ChatStreamResponse(
            StreamResponseAsync(
                systemPrompt,
                message,
                relevantChunks.Count > 0,
                toolCalls,
                cancellationToken),
            sources);
    }

    private async IAsyncEnumerable<ChatStreamEvent> StreamResponseAsync(
        string systemPrompt,
        string message,
        bool hasRelevantDocumentation,
        ICollection<ToolCallTrace> toolCalls,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var llmStopwatch = Stopwatch.StartNew();
        using var activity = IncidentCopilotTelemetry.ActivitySource
            .StartActivity(
                "incidentcopilot.agent.run",
                ActivityKind.Internal);
        activity?.SetTag(
            "rag.has_relevant_documentation",
            hasRelevantDocumentation);

        var completedRounds = 0;
        var outcome = "error";
        var toolExchanges = new List<LlmToolExchange>();

        try
        {
            var toolDefinitions = _agentTools.Values
                .Select(tool => tool.Definition)
                .ToArray();

            for (var round = 1; round <= MaxAgentRounds; round++)
            {
                completedRounds = round;
                _logger.LogInformation(
                    "Agent round {AgentRound}/{MaxAgentRounds} started. Completed tool calls: {CompletedToolCallCount}.",
                    round,
                    MaxAgentRounds,
                    toolExchanges.Count);

                LlmToolCall? requestedToolCall = null;
                var streamedContent = false;

                await foreach (var streamEvent in _llmClient
                    .GenerateAgentRoundStreamAsync(
                        new LlmAgentRoundRequest(
                            systemPrompt,
                            message,
                            toolDefinitions,
                            toolExchanges.ToArray()),
                        cancellationToken)
                    .WithCancellation(cancellationToken))
                {
                    if (!string.IsNullOrEmpty(streamEvent.Content))
                    {
                        streamedContent = true;
                        yield return new ChatDeltaEvent(streamEvent.Content);
                    }

                    if (streamEvent.ToolCall is not null)
                    {
                        if (requestedToolCall is not null)
                        {
                            throw new InvalidOperationException(
                                "The LLM requested multiple tool calls in one round, but parallel tool calls are disabled.");
                        }

                        requestedToolCall = streamEvent.ToolCall;
                    }
                }

                if (requestedToolCall is null)
                {
                    if (!streamedContent)
                    {
                        throw new InvalidOperationException(
                            "The LLM returned neither message content nor a tool call.");
                    }

                    if (toolExchanges.Count > 0)
                    {
                        _logger.LogInformation(
                            "Response path: agent loop completed in {AgentRound} round(s) after {ToolCallCount} tool call(s).",
                            round,
                            toolExchanges.Count);
                    }
                    else
                    {
                        _logger.LogInformation(
                            hasRelevantDocumentation
                                ? "Response path: RAG. No tool was requested."
                                : "Response path: scope refusal. No RAG context or tool call was available.");
                    }

                    outcome = "success";
                    yield break;
                }

                if (streamedContent)
                {
                    throw new InvalidOperationException(
                        "The LLM returned both message content and a tool call in the same round.");
                }

                if (round == MaxAgentRounds)
                {
                    _logger.LogWarning(
                        "Agent stopped after reaching the maximum of {MaxAgentRounds} rounds. The final requested tool {ToolName} was not executed.",
                        MaxAgentRounds,
                        requestedToolCall.Name);
                    outcome = "round_limit";
                    yield return new ChatDeltaEvent(
                        AgentRoundLimitMessage);
                    yield break;
                }

                if (!_agentTools.TryGetValue(
                        requestedToolCall.Name,
                        out var requestedTool))
                {
                    throw new InvalidOperationException(
                        $"Unknown tool requested: {requestedToolCall.Name}");
                }

                _logger.LogInformation(
                    """
                    Agent round: {AgentRound}/{MaxAgentRounds}
                    Tool requested: {ToolName}

                    Arguments:
                    {ToolArguments}
                    """,
                    round,
                    MaxAgentRounds,
                    requestedToolCall.Name,
                    FormatJson(requestedToolCall.ArgumentsJson));

                if (requestedTool.ExecutionMode ==
                    ToolExecutionMode.RequiresApproval)
                {
                    if (_approvalPolicies.TryGetValue(
                            requestedTool.Name,
                            out var approvalPolicy))
                    {
                        var decision = approvalPolicy.Evaluate(
                            requestedToolCall.ArgumentsJson,
                            toolExchanges);

                        if (!decision.Allowed)
                        {
                            _logger.LogInformation(
                                "Tool proposal {ToolName} was rejected by policy: {RejectionReason}",
                                requestedTool.Name,
                                decision.Reason);

                            toolExchanges.Add(new LlmToolExchange(
                                requestedToolCall,
                                JsonSerializer.Serialize(new
                                {
                                    approvalAllowed = false,
                                    reason = decision.Reason,
                                    responseInstruction =
                                        "Answer the user directly from the runtime evidence. " +
                                        "Do not mention the attempted tool call, approval " +
                                        "policy, rejection, or internal thresholds."
                                })));

                            continue;
                        }
                    }

                    var pendingAction = await _pendingActionService
                        .CreateAsync(
                            requestedTool.Name,
                            requestedToolCall.ArgumentsJson,
                            requestedTool.GetApprovalDescription(
                                requestedToolCall.ArgumentsJson),
                            cancellationToken);

                    _logger.LogInformation(
                        "Tool {ToolName} requires approval. Pending action {PendingActionId} was created.",
                        requestedTool.Name,
                        pendingAction.Id);

                    outcome = "pending_approval";
                    yield return new ChatPendingActionEvent(
                        pendingAction);
                    yield break;
                }

                ToolResult toolResult;

                try
                {
                    toolResult = await _toolExecutor.ExecuteAsync(
                        requestedTool,
                        new ToolExecutionContext(
                            requestedToolCall.ArgumentsJson),
                        cancellationToken);
                }
                catch (Exception exception)
                    when (exception is not OperationCanceledException)
                {
                    _logger.LogError(
                        exception,
                        "Tool execution threw an exception: {ToolName}.",
                        requestedToolCall.Name);
                    toolResult = new ToolResult(
                        false,
                        string.Empty,
                        exception.Message);
                }

                toolCalls.Add(new ToolCallTrace(
                    requestedToolCall.Name,
                    requestedToolCall.ArgumentsJson,
                    toolResult.Success,
                    toolResult.Error));

                _logger.LogInformation(
                    "Tool completed: {ToolName}. Success: {ToolSucceeded}.",
                    requestedToolCall.Name,
                    toolResult.Success);

                var serializedToolResult = toolResult.Success
                    ? toolResult.Content
                    : JsonSerializer.Serialize(new
                    {
                        error = toolResult.Error ?? "The tool failed without an error message."
                    });

                toolExchanges.Add(new LlmToolExchange(
                    requestedToolCall,
                    serializedToolResult));
            }
        }
        finally
        {
            llmStopwatch.Stop();

            if (cancellationToken.IsCancellationRequested)
            {
                outcome = "cancelled";
            }

            activity?.SetTag("agent.outcome", outcome);
            activity?.SetTag("agent.round_count", completedRounds);
            activity?.SetTag("agent.tool_call_count", toolExchanges.Count);

            if (outcome is not "success")
            {
                activity?.SetStatus(ActivityStatusCode.Error);
            }

            var tags = new TagList
            {
                { "agent.outcome", outcome }
            };

            IncidentCopilotTelemetry.AgentRuns.Add(1, tags);
            IncidentCopilotTelemetry.AgentDuration.Record(
                llmStopwatch.Elapsed.TotalSeconds,
                tags);

            _logger.LogInformation(
                "LLM duration: {LlmDurationMs:F0} ms.",
                llmStopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static string FormatJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(
                document.RootElement,
                IndentedJsonOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private void LogRetrievedChunks(
        IReadOnlyList<RetrievedChunk> chunks)
    {
        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            var survivesThreshold = chunk.Distance <= MaxDistance;

            _logger.LogInformation(
                """
                Chunk {ChunkRank}
                {FileName}
                Section: {Section}
                Distance: {Distance:F3}
                Survives threshold: {SurvivesThreshold}
                """,
                index + 1,
                chunk.FileName,
                chunk.Section ?? "Unknown",
                chunk.Distance,
                survivesThreshold);

            if (_diagnosticsOptions.IncludeChunkContent)
            {
                _logger.LogInformation(
                    """
                    Chunk {ChunkRank} content preview ({ChunkContentLength} characters total)
                    {ChunkContentPreview}
                    """,
                    index + 1,
                    chunk.Content.Length,
                    ChatDiagnosticsFormatter.CreateContentPreview(
                        chunk.Content,
                        _diagnosticsOptions.ChunkContentLimit));
            }
        }
    }

    // This is the augmentation part of RAG.
    private static string BuildContext(
        IReadOnlyList<RetrievedChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return "No relevant internal documentation was found.";
        }

        return string.Join(
            "\n\n---\n\n",
            chunks.Select(chunk =>
                $"""
                Source: {chunk.FileName}
                Section: {chunk.Section ?? "Unknown"}

                {chunk.Content}
                """));
    }

    private static string BuildSystemPrompt(string context)
    {
        return $"""
            You are an incident copilot. Your scope is limited to the monitored
            software systems and the internal documentation supplied below.

            Answer only from the supplied internal documentation or from tool
            results returned in this conversation. Do not answer unrelated
            questions or use general knowledge as a source.

            If the question is unrelated to this scope, or cannot be answered
            from the documentation or available tools, respond exactly:
            I can only help with questions about the monitored systems and the
            supplied internal documentation.

            You have access to read-only tools and tools that can change system
            state.

            Use read-only tools when you need current runtime evidence.

            If a state-changing tool is appropriate, you may request it, but
            the action requires explicit user approval before execution.

            Requesting an approval-required tool creates a pending proposal; it
            does not execute the state change. The application will show the
            user Confirm and Cancel controls for that proposal.

            When the user asks for a state change conditionally and the runtime
            evidence satisfies that condition, request the appropriate tool in
            the same agent run. Never ask the user to confirm in ordinary text,
            and never say "would you like me to" perform the action. Request the
            tool so the application can collect confirmation through its
            approval controls.

            If an approval policy rejects a proposal, do not request the same
            state-changing tool again. Treat that rejection as an internal
            control signal. Do not mention an attempted action, approval policy,
            policy rejection, guard, or internal threshold to the user. Instead,
            answer naturally from the runtime evidence and state that no
            incident is needed at this time.

            In the final answer, cite only evidence actually returned by tools
            in this agent run. Do not mention logs unless get_recent_logs
            completed successfully and returned those logs. Do not describe a
            metric as degraded when its returned value is within the healthy
            range.

            Do not claim that a state-changing action happened merely because
            you requested the tool. Only state that it completed after a
            successful confirmed execution result.

            Before proposing an incident, obtain current runtime evidence using
            read-only tools. Propose create_incident only when that evidence
            shows that a monitored service is unhealthy. Do not propose an
            incident when current evidence is healthy, unavailable, or based
            only on static documentation.

            You may use tools over multiple rounds. After each tool result,
            decide whether another tool is needed. When you have enough
            information, answer the user instead of requesting another tool.

            Never claim current metrics, logs, incidents, or system state unless
            that information was provided by a tool in this conversation.

            Internal documentation is static knowledge and must not be treated
            as current runtime state.

            Do not invent tool results.

            If a tool fails, times out, or reports that data was not found,
            clearly tell the user that the requested runtime information could
            not be retrieved. Never estimate, infer, or invent the missing
            runtime value.

            Internal documentation:

            {context}
            """;
    }
}
