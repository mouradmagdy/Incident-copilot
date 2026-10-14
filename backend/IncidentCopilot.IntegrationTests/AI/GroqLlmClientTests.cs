using System.Net;
using System.Text;
using System.Text.Json;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace IncidentCopilot.IntegrationTests.AI;

public sealed class GroqLlmClientTests
{
    [Fact]
    public async Task GenerateWithToolsAsync_PostsToolsAndReturnsToolCalls()
    {
        using var handler = new StubHttpMessageHandler(
            async (request, cancellationToken) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal(
                    "https://api.groq.test/openai/v1/chat/completions",
                    request.RequestUri?.AbsoluteUri);
                Assert.Equal(
                    "Bearer test-api-key",
                    request.Headers.Authorization?.ToString());

                var json = await request.Content!
                    .ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                Assert.Equal("openai/gpt-oss-120b", root.GetProperty("model").GetString());
                Assert.False(root.GetProperty("stream").GetBoolean());
                Assert.Equal("auto", root.GetProperty("tool_choice").GetString());
                Assert.False(root.GetProperty("parallel_tool_calls").GetBoolean());

                var messages = root.GetProperty("messages");
                Assert.Equal("system", messages[0].GetProperty("role").GetString());
                Assert.Equal("Be helpful.", messages[0].GetProperty("content").GetString());
                Assert.Equal("user", messages[1].GetProperty("role").GetString());
                Assert.Equal("Check orders.", messages[1].GetProperty("content").GetString());

                var tool = root.GetProperty("tools")[0];
                Assert.Equal("function", tool.GetProperty("type").GetString());
                var function = tool.GetProperty("function");
                Assert.Equal("get_service_metrics", function.GetProperty("name").GetString());
                Assert.Equal("Gets metrics.", function.GetProperty("description").GetString());
                Assert.Equal(
                    "object",
                    function.GetProperty("parameters").GetProperty("type").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "choices": [
                            {
                              "message": {
                                "role": "assistant",
                                "content": null,
                                "tool_calls": [
                                  {
                                    "id": "call_123",
                                    "type": "function",
                                    "function": {
                                      "name": "get_service_metrics",
                                      "arguments": "{\"serviceName\":\"OrderService\",\"minutes\":10}"
                                    }
                                  }
                                ]
                              }
                            }
                          ]
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
            });

        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);
        var request = new LlmToolRequest(
            "Be helpful.",
            "Check orders.",
            [
                new ToolDefinition(
                    "get_service_metrics",
                    "Gets metrics.",
                    new { type = "object" })
            ]);

        var result = await client.GenerateWithToolsAsync(
            request,
            CancellationToken.None);

        Assert.Null(result.Content);
        var toolCall = Assert.Single(result.ToolCalls);
        Assert.Equal("call_123", toolCall.Id);
        Assert.Equal("get_service_metrics", toolCall.Name);
        Assert.Equal(
            "{\"serviceName\":\"OrderService\",\"minutes\":10}",
            toolCall.ArgumentsJson);
    }

    [Fact]
    public async Task GenerateAfterToolAsync_PostsToolResultAndReturnsContent()
    {
        using var handler = new StubHttpMessageHandler(
            async (request, cancellationToken) =>
            {
                var json = await request.Content!
                    .ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                Assert.False(root.GetProperty("stream").GetBoolean());
                Assert.False(root.TryGetProperty("tools", out _));
                Assert.False(root.TryGetProperty("tool_choice", out _));

                var messages = root.GetProperty("messages");
                Assert.Equal(4, messages.GetArrayLength());
                Assert.Equal("system", messages[0].GetProperty("role").GetString());
                Assert.Equal("Be helpful.", messages[0].GetProperty("content").GetString());
                Assert.Equal("user", messages[1].GetProperty("role").GetString());
                Assert.Equal("Check orders.", messages[1].GetProperty("content").GetString());

                var assistantMessage = messages[2];
                Assert.Equal("assistant", assistantMessage.GetProperty("role").GetString());
                Assert.Equal(JsonValueKind.Null, assistantMessage.GetProperty("content").ValueKind);
                var postedToolCall = assistantMessage.GetProperty("tool_calls")[0];
                Assert.Equal("call_123", postedToolCall.GetProperty("id").GetString());
                Assert.Equal("function", postedToolCall.GetProperty("type").GetString());
                Assert.Equal(
                    "get_service_metrics",
                    postedToolCall.GetProperty("function").GetProperty("name").GetString());
                Assert.Equal(
                    "{\"serviceName\":\"OrderService\",\"minutes\":10}",
                    postedToolCall.GetProperty("function").GetProperty("arguments").GetString());

                var toolMessage = messages[3];
                Assert.Equal("tool", toolMessage.GetProperty("role").GetString());
                Assert.Equal("call_123", toolMessage.GetProperty("tool_call_id").GetString());
                Assert.Equal("get_service_metrics", toolMessage.GetProperty("name").GetString());
                Assert.Equal(
                    "{\"averageLatencyMs\":42}",
                    toolMessage.GetProperty("content").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "choices": [
                            {
                              "message": {
                                "role": "assistant",
                                "content": "OrderService latency is currently 42 ms."
                              }
                            }
                          ]
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
            });

        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);
        var result = await client.GenerateAfterToolAsync(
            new LlmToolContinuationRequest(
                "Be helpful.",
                "Check orders.",
                new LlmToolCall(
                    "call_123",
                    "get_service_metrics",
                    "{\"serviceName\":\"OrderService\",\"minutes\":10}"),
                "{\"averageLatencyMs\":42}"),
            CancellationToken.None);

        Assert.Equal("OrderService latency is currently 42 ms.", result.Content);
    }

    [Fact]
    public async Task GenerateWithToolsStreamAsync_CombinesToolCallDeltas()
    {
        using var handler = new StubHttpMessageHandler(
            async (request, cancellationToken) =>
            {
                var json = await request.Content!
                    .ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(json);
                Assert.True(document.RootElement.GetProperty("stream").GetBoolean());
                Assert.Equal(
                    "auto",
                    document.RootElement.GetProperty("tool_choice").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        data: {"choices":[{"delta":{"tool_calls":[{"index":0,"id":"call_123","type":"function","function":{"name":"get_service_metrics","arguments":"{\"service"}}]}}]}

                        data: {"choices":[{"delta":{"tool_calls":[{"index":0,"function":{"arguments":"Name\":\"OrderService\",\"minutes\":15}"}}]}}]}

                        data: [DONE]

                        """,
                        Encoding.UTF8,
                        "text/event-stream")
                };
            });

        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);
        var events = await ReadAllAsync(
            client.GenerateWithToolsStreamAsync(
                new LlmToolRequest(
                    "system",
                    "user",
                    [
                        new ToolDefinition(
                            "get_service_metrics",
                            "Gets metrics.",
                            new { type = "object" })
                    ]),
                CancellationToken.None));

        var streamEvent = Assert.Single(events);
        Assert.Null(streamEvent.Content);
        Assert.NotNull(streamEvent.ToolCall);
        Assert.Equal("call_123", streamEvent.ToolCall.Id);
        Assert.Equal("get_service_metrics", streamEvent.ToolCall.Name);
        Assert.Equal(
            "{\"serviceName\":\"OrderService\",\"minutes\":15}",
            streamEvent.ToolCall.ArgumentsJson);
    }

    [Fact]
    public async Task GenerateAfterToolStreamAsync_ReturnsContentDeltas()
    {
        using var handler = new StubHttpMessageHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        data: {"choices":[{"delta":{"content":"OrderService "}}]}

                        data: {"choices":[{"delta":{"content":"is healthy."}}]}

                        data: [DONE]

                        """,
                        Encoding.UTF8,
                        "text/event-stream")
                }));

        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);
        var content = await ReadAllAsync(
            client.GenerateAfterToolStreamAsync(
                new LlmToolContinuationRequest(
                    "system",
                    "user",
                    new LlmToolCall(
                        "call_123",
                        "get_service_metrics",
                        "{\"serviceName\":\"OrderService\"}"),
                    "{\"averageLatencyMs\":42}"),
                CancellationToken.None));

        Assert.Equal(["OrderService ", "is healthy."], content);
    }

    [Fact]
    public async Task GenerateWithToolsAsync_PropagatesCancellationToken()
    {
        using var handler = new StubHttpMessageHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GenerateWithToolsAsync(
                new LlmToolRequest("system", "user", []),
                cancellation.Token));
    }

    private static GroqLlmClient CreateClient(HttpClient httpClient)
    {
        return new GroqLlmClient(
            httpClient,
            Options.Create(new GroqOptions
            {
                ApiKey = "test-api-key",
                BaseUrl = "https://api.groq.test/openai/v1",
                Model = "openai/gpt-oss-120b"
            }));
    }

    private static async Task<IReadOnlyList<T>> ReadAllAsync<T>(
        IAsyncEnumerable<T> source)
    {
        var result = new List<T>();
        await foreach (var item in source)
        {
            result.Add(item);
        }

        return result;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
            sendAsync)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return sendAsync(request, cancellationToken);
        }
    }
}
