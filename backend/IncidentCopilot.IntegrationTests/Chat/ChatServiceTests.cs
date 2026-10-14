using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.Chat;
using IncidentCopilot.Application.Knowledge;
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
            await ReadAllAsync(response.Content));
        Assert.Empty(response.Sources);
        Assert.NotNull(llmClient.InitialRequest);
        Assert.Contains(
            "No relevant internal documentation was found.",
            llmClient.InitialRequest.SystemPrompt);
        Assert.Contains(
            "Your scope is limited to the monitored",
            llmClient.InitialRequest.SystemPrompt);
        Assert.Equal(0, tool.ExecutionCount);
    }

    [Fact]
    public async Task AskStreamAsync_WhenToolIsRequested_ExecutesItAndContinuesLlmTurn()
    {
        const string arguments =
            "{\"serviceName\":\"OrderService\",\"minutes\":10}";
        const string toolContent = "{\"averageLatencyMs\":42}";
        var toolCall = new LlmToolCall(
            "call_123",
            "get_service_metrics",
            arguments);
        var llmClient = new FakeLlmClient(
            new LlmTurnResponse(null, [toolCall]),
            new LlmResponse("OrderService latency is currently 42 ms."));
        var tool = new FakeAgentTool(
            new ToolResult(true, toolContent));
        var service = CreateService(llmClient, tool);

        var response = await service.AskStreamAsync(
            "What is the current OrderService latency?",
            CancellationToken.None);

        Assert.Equal(
            ["OrderService latency is currently 42 ms."],
            await ReadAllAsync(response.Content));
        Assert.Equal(1, tool.ExecutionCount);
        Assert.Equal(arguments, tool.LastArgumentsJson);
        Assert.NotNull(llmClient.ContinuationRequest);
        Assert.Equal(toolCall, llmClient.ContinuationRequest.ToolCall);
        Assert.Equal(toolContent, llmClient.ContinuationRequest.ToolResult);
    }

    private static ChatService CreateService(
        ILlmClient llmClient,
        IAgentTool tool)
    {
        return new ChatService(
            new EmptyKnowledgeRetriever(),
            llmClient,
            tool,
            NullLogger<ChatService>.Instance,
            Options.Create(new ChatDiagnosticsOptions()));
    }

    private static async Task<IReadOnlyList<string>> ReadAllAsync(
        IAsyncEnumerable<string> content)
    {
        var result = new List<string>();
        await foreach (var item in content)
        {
            result.Add(item);
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

        public FakeAgentTool(ToolResult? result = null)
        {
            _result = result ?? new ToolResult(true, "{}");
        }

        public string Name => "get_service_metrics";

        public ToolDefinition Definition => new(
            Name,
            "Gets current service metrics.",
            new { type = "object" });

        public int ExecutionCount { get; private set; }

        public string? LastArgumentsJson { get; private set; }

        public Task<ToolResult> ExecuteAsync(
            string argumentsJson,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            LastArgumentsJson = argumentsJson;
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeLlmClient : ILlmClient
    {
        private readonly LlmTurnResponse _initialResponse;
        private readonly LlmResponse? _continuationResponse;

        public FakeLlmClient(
            LlmTurnResponse initialResponse,
            LlmResponse? continuationResponse = null)
        {
            _initialResponse = initialResponse;
            _continuationResponse = continuationResponse;
        }

        public LlmToolRequest? InitialRequest { get; private set; }

        public LlmToolContinuationRequest? ContinuationRequest { get; private set; }

        public Task<LlmResponse> GenerateAsync(
            LlmRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<LlmTurnResponse> GenerateWithToolsAsync(
            LlmToolRequest request,
            CancellationToken cancellationToken)
        {
            InitialRequest = request;
            return Task.FromResult(_initialResponse);
        }

        public async IAsyncEnumerable<LlmStreamEvent> GenerateWithToolsStreamAsync(
            LlmToolRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            InitialRequest = request;

            if (!string.IsNullOrEmpty(_initialResponse.Content))
            {
                yield return new LlmStreamEvent(
                    _initialResponse.Content,
                    null);
            }

            foreach (var toolCall in _initialResponse.ToolCalls)
            {
                yield return new LlmStreamEvent(null, toolCall);
            }

            await Task.CompletedTask;
        }

        public Task<LlmResponse> GenerateAfterToolAsync(
            LlmToolContinuationRequest request,
            CancellationToken cancellationToken)
        {
            ContinuationRequest = request;
            return Task.FromResult(
                _continuationResponse ??
                throw new InvalidOperationException(
                    "No continuation response was configured."));
        }

        public async IAsyncEnumerable<string> GenerateAfterToolStreamAsync(
            LlmToolContinuationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ContinuationRequest = request;
            yield return (
                _continuationResponse ??
                throw new InvalidOperationException(
                    "No continuation response was configured."))
                .Content;
            await Task.CompletedTask;
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
