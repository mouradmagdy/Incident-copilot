using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.Chat;
using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Application.PendingActions;
using IncidentCopilot.Core.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace IncidentCopilot.IntegrationTests.Chat;

public sealed class ChatServiceTests
{
    [Fact]
    public async Task AskStreamAsync_WithoutRelevantDocumentation_StillCallsLlm()
    {
        const string refusal =
            "I can only help with questions about the monitored systems and the supplied internal documentation.";
        var llmClient = new FakeLlmClient(
            new LlmTurnResponse(refusal, []));
        var tool = new FakeAgentTool();
        var service = CreateService(llmClient, tool);

        var response = await service.AskStreamAsync(
            "How do I cook pasta?",
            CancellationToken.None);

        Assert.Equal(
            [refusal],
            await ReadAllAsync(response.Events));
        Assert.Empty(response.Sources);
        var initialRequest = Assert.Single(llmClient.AgentRoundRequests);
        Assert.Contains(
            "No relevant internal documentation was found.",
            initialRequest.SystemPrompt);
        Assert.Contains(
            "Your scope is limited to the monitored",
            initialRequest.SystemPrompt);
        Assert.Contains(
            "Never ask the user to confirm in ordinary text",
            initialRequest.SystemPrompt);
        Assert.Contains(
            "Confirm and Cancel controls",
            initialRequest.SystemPrompt);
        Assert.Contains(
            "Treat that rejection as an internal",
            initialRequest.SystemPrompt);
        Assert.Contains(
            "Do not mention logs unless get_recent_logs",
            initialRequest.SystemPrompt);
        Assert.Equal(0, tool.ExecutionCount);
    }

    [Fact]
    public async Task AskStreamAsync_WhenSecondToolIsRequested_ExecutesMatchingTool()
    {
        const string arguments =
            "{\"serviceName\":\"OrderService\",\"minutes\":10}";
        const string toolContent =
            "{\"logs\":[{\"level\":\"Error\",\"message\":\"Database timeout.\"}]}";
        var toolCall = new LlmToolCall(
            "call_123",
            "get_recent_logs",
            arguments);
        var llmClient = new FakeLlmClient(
            new LlmTurnResponse(null, [toolCall]),
            new LlmTurnResponse("OrderService has recent error logs.", []));
        var metricsTool = new FakeAgentTool("get_service_metrics");
        var recentLogsTool = new FakeAgentTool(
            "get_recent_logs",
            new ToolResult(true, toolContent));
        var service = CreateService(
            llmClient,
            metricsTool,
            recentLogsTool);

        var response = await service.AskStreamAsync(
            "Show me the recent OrderService logs.",
            CancellationToken.None);

        Assert.Equal(
            ["OrderService has recent error logs."],
            await ReadAllAsync(response.Events));
        Assert.Equal(0, metricsTool.ExecutionCount);
        Assert.Equal(1, recentLogsTool.ExecutionCount);
        Assert.Equal(arguments, recentLogsTool.LastArgumentsJson);
        Assert.Equal(2, llmClient.AgentRoundRequests.Count);
        Assert.Equal(
            ["get_service_metrics", "get_recent_logs"],
            llmClient.AgentRoundRequests[0].Tools.Select(tool => tool.Name));
        var exchange = Assert.Single(
            llmClient.AgentRoundRequests[1].ToolExchanges);
        Assert.Equal(toolCall, exchange.ToolCall);
        Assert.Equal(toolContent, exchange.ToolResult);
    }

    [Fact]
    public async Task RunAgentAsync_MultiStepLoop_ReturnsAnswerAndToolTrace()
    {
        var metricsCall = new LlmToolCall(
            "call_metrics",
            "get_service_metrics",
            "{\"serviceName\":\"OrderService\",\"minutes\":15}");
        var logsCall = new LlmToolCall(
            "call_logs",
            "get_recent_logs",
            "{\"serviceName\":\"OrderService\",\"minutes\":15}");
        var llmClient = new FakeLlmClient(
            new LlmTurnResponse(null, [metricsCall]),
            new LlmTurnResponse(null, [logsCall]),
            new LlmTurnResponse(
                "High latency correlates with database timeout logs.",
                []));
        var metricsTool = new FakeAgentTool(
            "get_service_metrics",
            new ToolResult(true, "{\"averageLatencyMs\":1800}"));
        var logsTool = new FakeAgentTool(
            "get_recent_logs",
            new ToolResult(true, "{\"logs\":[{\"message\":\"Database timeout.\"}]}"));
        var service = CreateService(llmClient, metricsTool, logsTool);

        var result = await service.RunAgentAsync(
            "Investigate the current OrderService latency and errors.",
            CancellationToken.None);

        Assert.Equal(
            "High latency correlates with database timeout logs.",
            result.Content);
        Assert.Equal(2, result.ToolCalls.Count);
        Assert.Equal("get_service_metrics", result.ToolCalls[0].ToolName);
        Assert.Equal(metricsCall.ArgumentsJson, result.ToolCalls[0].ArgumentsJson);
        Assert.True(result.ToolCalls[0].Success);
        Assert.Null(result.ToolCalls[0].Error);
        Assert.Equal("get_recent_logs", result.ToolCalls[1].ToolName);
        Assert.Equal(logsCall.ArgumentsJson, result.ToolCalls[1].ArgumentsJson);
        Assert.True(result.ToolCalls[1].Success);
        Assert.Null(result.ToolCalls[1].Error);
        Assert.Equal(1, metricsTool.ExecutionCount);
        Assert.Equal(1, logsTool.ExecutionCount);
        Assert.Equal(3, llmClient.AgentRoundRequests.Count);
        Assert.Empty(llmClient.AgentRoundRequests[0].ToolExchanges);
        Assert.Single(llmClient.AgentRoundRequests[1].ToolExchanges);
        Assert.Equal(2, llmClient.AgentRoundRequests[2].ToolExchanges.Count);
        Assert.Equal(
            ["get_service_metrics", "get_recent_logs"],
            llmClient.AgentRoundRequests[2]
                .ToolExchanges
                .Select(exchange => exchange.ToolCall.Name));
    }

    [Fact]
    public async Task RunAgentAsync_WhenToolFails_RecordsErrorInTrace()
    {
        var toolCall = new LlmToolCall(
            "call_logs",
            "get_recent_logs",
            "{\"serviceName\":\"OrderService\"}");
        var llmClient = new FakeLlmClient(
            new LlmTurnResponse(null, [toolCall]),
            new LlmTurnResponse("No recent logs were available.", []));
        var logsTool = new FakeAgentTool(
            "get_recent_logs",
            new ToolResult(
                false,
                string.Empty,
                "No logs were found."));
        var service = CreateService(llmClient, logsTool);

        var result = await service.RunAgentAsync(
            "Show recent OrderService logs.",
            CancellationToken.None);

        var trace = Assert.Single(result.ToolCalls);
        Assert.Equal("get_recent_logs", trace.ToolName);
        Assert.Equal(toolCall.ArgumentsJson, trace.ArgumentsJson);
        Assert.False(trace.Success);
        Assert.Equal("No logs were found.", trace.Error);
        var exchange = Assert.Single(
            llmClient.AgentRoundRequests[1].ToolExchanges);
        Assert.Equal(
            "{\"error\":\"No logs were found.\"}",
            exchange.ToolResult);
    }

    [Fact]
    public async Task AskStreamAsync_WhenFiveRoundsRequestTools_StopsWithoutSixthRound()
    {
        var roundResponses = Enumerable.Range(1, 5)
            .Select(round => new LlmTurnResponse(
                null,
                [
                    new LlmToolCall(
                        $"call_{round}",
                        "get_service_metrics",
                        "{\"serviceName\":\"OrderService\"}")
                ]))
            .ToArray();
        var llmClient = new FakeLlmClient(roundResponses);
        var tool = new FakeAgentTool();
        var service = CreateService(llmClient, tool);

        var response = await service.AskStreamAsync(
            "Keep investigating OrderService.",
            CancellationToken.None);

        Assert.Equal(
            ["I couldn't complete the investigation within the maximum of 5 agent rounds."],
            await ReadAllAsync(response.Events));
        Assert.Equal(5, llmClient.AgentRoundRequests.Count);
        Assert.Equal(4, tool.ExecutionCount);
        Assert.Equal(
            [0, 1, 2, 3, 4],
            llmClient.AgentRoundRequests
                .Select(request => request.ToolExchanges.Count));
    }

    [Fact]
    public async Task RunAgentAsync_WhenUnhealthyMetricsLeadToIncidentProposal_ReturnsPendingActionWithoutExecutingIt()
    {
        var metricsCall = new LlmToolCall(
            "call_metrics",
            "get_service_metrics",
            "{\"serviceName\":\"OrderService\"}");
        var createIncidentCall = new LlmToolCall(
            "call_incident",
            "create_incident",
            "{\"serviceName\":\"OrderService\",\"title\":\"OrderService error spike\",\"description\":\"Error rate is 18%.\",\"severity\":\"High\"}");
        var llmClient = new FakeLlmClient(
            new LlmTurnResponse(null, [metricsCall]),
            new LlmTurnResponse(null, [createIncidentCall]));
        var metricsTool = new FakeAgentTool(
            "get_service_metrics",
            new ToolResult(
                true,
                "{\"serviceName\":\"OrderService\",\"averageLatencyMs\":1800,\"p95LatencyMs\":3200,\"errorRatePercent\":18,\"queueDepth\":80,\"activeDbConnections\":98}"));
        var createIncidentTool = new FakeAgentTool(
            "create_incident",
            executionMode: ToolExecutionMode.RequiresApproval);
        var pendingActions = new FakePendingActionService();
        var service = CreateService(
            llmClient,
            pendingActions,
            metricsTool,
            createIncidentTool);

        var result = await service.RunAgentAsync(
            "Investigate OrderService and create an incident if it is unhealthy.",
            CancellationToken.None);

        Assert.Equal(1, metricsTool.ExecutionCount);
        Assert.Equal(0, createIncidentTool.ExecutionCount);
        Assert.Single(result.ToolCalls);
        Assert.NotNull(result.PendingAction);
        Assert.Equal("create_incident", result.PendingAction.ToolName);
        Assert.Equal(
            llmClient.ApprovalExplanation,
            result.Content);
        var explanationRequest = Assert.Single(
            llmClient.GenerateRequests);
        Assert.Contains(
            "errorRatePercent",
            explanationRequest.UserMessage);
        Assert.Contains(
            "do not invent values",
            explanationRequest.SystemPrompt);
        var pendingActionRequest = Assert.Single(
            pendingActions.CreatedActions);
        Assert.Equal(
            createIncidentCall.ArgumentsJson,
            pendingActionRequest.ArgumentsJson);
        Assert.Equal(
            createIncidentCall.Id,
            pendingActionRequest.ToolCallId);
        Assert.Single(pendingActionRequest.ToolExchanges);
    }

    [Fact]
    public async Task RunAgentAsync_WhenMetricsAreHealthy_DoesNotProposeIncident()
    {
        var metricsCall = new LlmToolCall(
            "call_metrics",
            "get_service_metrics",
            "{\"serviceName\":\"OrderService\"}");
        var llmClient = new FakeLlmClient(
            new LlmTurnResponse(null, [metricsCall]),
            new LlmTurnResponse(
                "OrderService is healthy; no incident is needed.",
                []));
        var metricsTool = new FakeAgentTool(
            "get_service_metrics",
            new ToolResult(true, "{\"errorRatePercent\":0.1}"));
        var createIncidentTool = new FakeAgentTool(
            "create_incident",
            executionMode: ToolExecutionMode.RequiresApproval);
        var pendingActions = new FakePendingActionService();
        var service = CreateService(
            llmClient,
            pendingActions,
            metricsTool,
            createIncidentTool);

        var result = await service.RunAgentAsync(
            "Investigate OrderService and create an incident if it is unhealthy.",
            CancellationToken.None);

        Assert.Equal(
            "OrderService is healthy; no incident is needed.",
            result.Content);
        Assert.Equal(1, metricsTool.ExecutionCount);
        Assert.Equal(0, createIncidentTool.ExecutionCount);
        Assert.Null(result.PendingAction);
        Assert.Empty(pendingActions.CreatedActions);
        Assert.Empty(llmClient.GenerateRequests);
    }

    [Fact]
    public async Task RunAgentAsync_WhenModelProposesIncidentForHealthyMetrics_PolicyRejectsProposal()
    {
        var metricsCall = new LlmToolCall(
            "call_metrics",
            "get_service_metrics",
            "{\"serviceName\":\"OrderService\"}");
        var createIncidentCall = new LlmToolCall(
            "call_incident",
            "create_incident",
            "{\"serviceName\":\"OrderService\",\"title\":\"OrderService issue\",\"severity\":\"High\"}");
        var llmClient = new FakeLlmClient(
            new LlmTurnResponse(null, [metricsCall]),
            new LlmTurnResponse(null, [createIncidentCall]),
            new LlmTurnResponse(
                "OrderService is healthy, so no incident was proposed.",
                []));
        var metricsTool = new FakeAgentTool(
            "get_service_metrics",
            new ToolResult(
                true,
                "{\"serviceName\":\"OrderService\",\"averageLatencyMs\":220,\"p95LatencyMs\":410,\"errorRatePercent\":0.4,\"queueDepth\":12,\"activeDbConnections\":25}"));
        var createIncidentTool = new FakeAgentTool(
            "create_incident",
            executionMode: ToolExecutionMode.RequiresApproval);
        var pendingActions = new FakePendingActionService();
        var service = CreateService(
            llmClient,
            pendingActions,
            metricsTool,
            createIncidentTool);

        var result = await service.RunAgentAsync(
            "Investigate OrderService and create an incident if it is unhealthy.",
            CancellationToken.None);

        Assert.Equal(
            "OrderService is healthy, so no incident was proposed.",
            result.Content);
        Assert.Null(result.PendingAction);
        Assert.Empty(pendingActions.CreatedActions);
        Assert.Empty(llmClient.GenerateRequests);
        Assert.Equal(0, createIncidentTool.ExecutionCount);
        Assert.Equal(3, llmClient.AgentRoundRequests.Count);
        var rejection = Assert.Single(
            llmClient.AgentRoundRequests[2].ToolExchanges,
            exchange =>
                exchange.ToolCall.Name == "create_incident");
        Assert.Contains("approvalAllowed", rejection.ToolResult);
        Assert.Contains("false", rejection.ToolResult);
        Assert.Contains(
            "Do not mention the attempted tool call",
            rejection.ToolResult);
    }

    private static ChatService CreateService(
        ILlmClient llmClient,
        params IAgentTool[] tools)
    {
        return CreateService(
            llmClient,
            new FakePendingActionService(),
            tools);
    }

    private static ChatService CreateService(
        ILlmClient llmClient,
        IPendingActionService pendingActionService,
        params IAgentTool[] tools)
    {
        return new ChatService(
            new EmptyKnowledgeRetriever(),
            llmClient,
            tools,
            [new CreateIncidentApprovalPolicy()],
            new ToolExecutor(),
            pendingActionService,
            NullLogger<ChatService>.Instance,
            Options.Create(new ChatDiagnosticsOptions()));
    }

    private static async Task<IReadOnlyList<string>> ReadAllAsync(
        IAsyncEnumerable<ChatStreamEvent> events)
    {
        var result = new List<string>();
        await foreach (var item in events)
        {
            if (item is ChatDeltaEvent delta)
            {
                result.Add(delta.Content);
            }
        }

        return result;
    }

    private sealed class EmptyKnowledgeRetriever : IKnowledgeRetriever
    {
        public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
            string query,
            int topK,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RetrievedChunk>>([]);
        }
    }

    private sealed class FakeAgentTool : IAgentTool
    {
        private readonly ToolResult _result;

        public FakeAgentTool(
            string name = "get_service_metrics",
            ToolResult? result = null,
            ToolExecutionMode executionMode =
                ToolExecutionMode.ReadOnly)
        {
            Name = name;
            _result = result ?? new ToolResult(true, "{}");
            ExecutionMode = executionMode;
        }

        public string Name { get; }

        public ToolDefinition Definition => new(
            Name,
            "Gets current service metrics.",
            new { type = "object" });

        public ToolExecutionMode ExecutionMode { get; }

        public int ExecutionCount { get; private set; }

        public string? LastArgumentsJson { get; private set; }

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            LastArgumentsJson = context.ArgumentsJson;
            return Task.FromResult(_result);
        }
    }

    private sealed class FakePendingActionService
        : IPendingActionService
    {
        public List<PendingActionRequest> CreatedActions { get; } = [];

        public Task<PendingActionView> CreateAsync(
            PendingActionRequest request,
            CancellationToken cancellationToken = default)
        {
            var action = new PendingActionView(
                Guid.NewGuid(),
                request.ToolName,
                request.Description,
                "Pending",
                DateTime.UtcNow,
                null,
                null,
                null,
                null);

            CreatedActions.Add(request);

            return Task.FromResult(action);
        }

        public Task<PendingActionView?> ConfirmAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PendingActionView?> CancelAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeLlmClient : ILlmClient
    {
        private readonly Queue<LlmTurnResponse> _roundResponses;

        public FakeLlmClient(params LlmTurnResponse[] roundResponses)
        {
            _roundResponses = new Queue<LlmTurnResponse>(roundResponses);
        }

        public List<LlmAgentRoundRequest> AgentRoundRequests { get; } = [];

        public List<LlmRequest> GenerateRequests { get; } = [];

        public string ApprovalExplanation { get; set; } =
            "The runtime evidence shows serious degradation, so I am proposing an incident for your approval.";

        public Task<LlmResponse> GenerateAsync(
            LlmRequest request,
            CancellationToken cancellationToken)
        {
            GenerateRequests.Add(request);
            return Task.FromResult(new LlmResponse(
                ApprovalExplanation));
        }

        public Task<LlmTurnResponse> GenerateWithToolsAsync(
            LlmToolRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<LlmStreamEvent> GenerateWithToolsStreamAsync(
            LlmToolRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public async IAsyncEnumerable<LlmStreamEvent> GenerateAgentRoundStreamAsync(
            LlmAgentRoundRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            AgentRoundRequests.Add(request);
            var response = _roundResponses.Count > 0
                ? _roundResponses.Dequeue()
                : throw new InvalidOperationException(
                    "No agent round response was configured.");

            if (!string.IsNullOrEmpty(response.Content))
            {
                yield return new LlmStreamEvent(response.Content, null);
            }

            foreach (var toolCall in response.ToolCalls)
            {
                yield return new LlmStreamEvent(null, toolCall);
            }

            await Task.CompletedTask;
        }

        public Task<LlmResponse> GenerateAfterToolAsync(
            LlmToolContinuationRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<string> GenerateAfterToolStreamAsync(
            LlmToolContinuationRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public async IAsyncEnumerable<string> GenerateStreamAsync(
            LlmRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
