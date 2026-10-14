using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Core.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace IncidentCopilot.Application.Chat;

public sealed class ChatService : IChatService
{
    private const int TopK = 4;
    private const double MaxDistance = 0.75;
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IKnowledgeRetriever _retriever;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<ChatService> _logger;
    private readonly ChatDiagnosticsOptions _diagnosticsOptions;
    private readonly IAgentTool _agentTool;

    public ChatService(
        IKnowledgeRetriever retriever,
        ILlmClient llmClient,
        IAgentTool agentTool,
        ILogger<ChatService> logger,
        IOptions<ChatDiagnosticsOptions> diagnosticsOptions)
    {
        _retriever = retriever;
        _llmClient = llmClient;
        _logger = logger;
        _diagnosticsOptions = diagnosticsOptions.Value;
        _agentTool = agentTool;
    }

    public async Task<ChatStreamResponse> AskStreamAsync(
        string message,
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
                cancellationToken),
            sources);
    }

    private async IAsyncEnumerable<string> StreamResponseAsync(
        string systemPrompt,
        string message,
        bool hasRelevantDocumentation,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var llmStopwatch = Stopwatch.StartNew();

        try
        {
            var tool = _agentTool;
            LlmToolCall? requestedToolCall = null;
            var streamedContent = false;

            await foreach (var streamEvent in _llmClient
                .GenerateWithToolsStreamAsync(
                    new LlmToolRequest(
                        systemPrompt,
                        message,
                        [tool.Definition]),
                    cancellationToken)
                .WithCancellation(cancellationToken))
            {
                if (!string.IsNullOrEmpty(streamEvent.Content))
                {
                    streamedContent = true;
                    yield return streamEvent.Content;
                }

                if (streamEvent.ToolCall is not null)
                {
                    if (requestedToolCall is not null)
                    {
                        throw new InvalidOperationException(
                            "The LLM requested multiple tool calls, but this chat flow supports one call per turn.");
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

                _logger.LogInformation(
                    hasRelevantDocumentation
                        ? "Response path: RAG. No tool was requested."
                        : "Response path: scope refusal. No RAG context or tool call was available.");

                yield break;
            }

            if (streamedContent)
            {
                throw new InvalidOperationException(
                    "The LLM returned both message content and a tool call in the same turn.");
            }

            if (!string.Equals(
                    requestedToolCall.Name,
                    tool.Definition.Name,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Unknown tool requested: {requestedToolCall.Name}");
            }

            _logger.LogInformation(
                """
                Tool requested: {ToolName}

                Arguments:
                {ToolArguments}
                """,
                requestedToolCall.Name,
                FormatJson(requestedToolCall.ArgumentsJson));

            var toolResult = await tool.ExecuteAsync(
                requestedToolCall.ArgumentsJson,
                cancellationToken);

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

            await foreach (var content in _llmClient
                .GenerateAfterToolStreamAsync(
                    new LlmToolContinuationRequest(
                        systemPrompt,
                        message,
                        requestedToolCall,
                        serializedToolResult),
                    cancellationToken)
                .WithCancellation(cancellationToken))
            {
                yield return content;
            }
        }
        finally
        {
            llmStopwatch.Stop();
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

            You have access to tools for obtaining live system information.

            Use tools when the user's question requires current runtime data.

            Never claim current metrics, logs, incidents, or system state unless
            that information was provided by a tool in this conversation.

            Internal documentation is static knowledge and must not be treated
            as current runtime state.

            Do not invent tool results.

            Internal documentation:

            {context}
            """;
    }
}
