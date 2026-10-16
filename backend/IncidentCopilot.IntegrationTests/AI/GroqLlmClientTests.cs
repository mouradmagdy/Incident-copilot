using System.Net;
using System.Text;
using System.Text.Json;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task GenerateAgentRoundStreamAsync_PostsFullToolHistory()
    {
        using var handler = new StubHttpMessageHandler(
            async (request, cancellationToken) =>
            {
                var json = await request.Content!
                    .ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                Assert.True(root.GetProperty("stream").GetBoolean());
                Assert.Equal("auto", root.GetProperty("tool_choice").GetString());
                Assert.False(root.GetProperty("parallel_tool_calls").GetBoolean());
                Assert.Equal(2, root.GetProperty("tools").GetArrayLength());

                var messages = root.GetProperty("messages");
                Assert.Equal(6, messages.GetArrayLength());
                Assert.Equal("system", messages[0].GetProperty("role").GetString());
                Assert.Equal("user", messages[1].GetProperty("role").GetString());
                Assert.Equal("assistant", messages[2].GetProperty("role").GetString());
                Assert.Equal(
                    "get_service_metrics",
                    messages[2]
                        .GetProperty("tool_calls")[0]
                        .GetProperty("function")
                        .GetProperty("name")
                        .GetString());
                Assert.Equal("tool", messages[3].GetProperty("role").GetString());
                Assert.Equal(
                    "call_metrics",
                    messages[3].GetProperty("tool_call_id").GetString());
                Assert.Equal("assistant", messages[4].GetProperty("role").GetString());
                Assert.Equal(
                    "get_recent_logs",
                    messages[4]
                        .GetProperty("tool_calls")[0]
                        .GetProperty("function")
                        .GetProperty("name")
                        .GetString());
                Assert.Equal("tool", messages[5].GetProperty("role").GetString());
                Assert.Equal(
                    "call_logs",
                    messages[5].GetProperty("tool_call_id").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        data: {"choices":[{"delta":{"content":"Investigation complete."}}]}

                        data: [DONE]

                        """,
                        Encoding.UTF8,
                        "text/event-stream")
                };
            });

        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);
        var events = await ReadAllAsync(
            client.GenerateAgentRoundStreamAsync(
                new LlmAgentRoundRequest(
                    "system",
                    "Investigate OrderService.",
                    [
                        new ToolDefinition(
                            "get_service_metrics",
                            "Gets metrics.",
                            new { type = "object" }),
                        new ToolDefinition(
                            "get_recent_logs",
                            "Gets logs.",
                            new { type = "object" })
                    ],
                    [
                        new LlmToolExchange(
                            new LlmToolCall(
                                "call_metrics",
                                "get_service_metrics",
                                "{\"serviceName\":\"OrderService\"}"),
                            "{\"averageLatencyMs\":1800}"),
                        new LlmToolExchange(
                            new LlmToolCall(
                                "call_logs",
                                "get_recent_logs",
                                "{\"serviceName\":\"OrderService\"}"),
                            "{\"logs\":[{\"message\":\"Database timeout.\"}]}")
                    ]),
                CancellationToken.None));

        var streamEvent = Assert.Single(events);
        Assert.Equal("Investigation complete.", streamEvent.Content);
        Assert.Null(streamEvent.ToolCall);
    }

    [Fact]
    public async Task GenerateAgentRoundStreamAsync_WithoutTools_RequestsFinalContentOnly()
    {
        using var handler = new StubHttpMessageHandler(
            async (request, cancellationToken) =>
            {
                var json = await request.Content!
                    .ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                Assert.False(root.TryGetProperty("tools", out _));
                Assert.False(root.TryGetProperty("tool_choice", out _));
                Assert.False(root.TryGetProperty(
                    "parallel_tool_calls",
                    out _));

                var messages = root.GetProperty("messages");
                Assert.Equal(4, messages.GetArrayLength());
                Assert.Equal(
                    "assistant",
                    messages[2].GetProperty("role").GetString());
                Assert.Equal(
                    "call_incident",
                    messages[2]
                        .GetProperty("tool_calls")[0]
                        .GetProperty("id")
                        .GetString());
                Assert.Equal(
                    "tool",
                    messages[3].GetProperty("role").GetString());
                Assert.Equal(
                    "call_incident",
                    messages[3].GetProperty("tool_call_id").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        data: {"choices":[{"delta":{"content":"Incident #3 was created successfully."}}]}

                        data: [DONE]

                        """,
                        Encoding.UTF8,
                        "text/event-stream")
                };
            });

        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);
        var events = await ReadAllAsync(
            client.GenerateAgentRoundStreamAsync(
                new LlmAgentRoundRequest(
                    "Return the confirmed result.",
                    "Create an incident if needed.",
                    [],
                    [
                        new LlmToolExchange(
                            new LlmToolCall(
                                "call_incident",
                                "create_incident",
                                "{\"serviceName\":\"OrderService\"}"),
                            "{\"id\":3,\"status\":\"Open\"}")
                    ]),
                CancellationToken.None));

        var streamEvent = Assert.Single(events);
        Assert.Equal(
            "Incident #3 was created successfully.",
            streamEvent.Content);
        Assert.Null(streamEvent.ToolCall);
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

    [Fact]
    public async Task GenerateAgentRoundStreamAsync_WhenRateLimited_RetriesAfterHeader()
    {
        var requestCount = 0;
        using var handler = new StubHttpMessageHandler(
            (_, _) =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    var limitedResponse = new HttpResponseMessage(
                        HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent(
                            """
                            {
                              "error": {
                                "message": "Please try again.",
                                "type": "tokens"
                              }
                            }
                            """,
                            Encoding.UTF8,
                            "application/json")
                    };
                    limitedResponse.Headers.TryAddWithoutValidation(
                        "retry-after",
                        "0");
                    return Task.FromResult(limitedResponse);
                }

                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            """
                            data: {"choices":[{"delta":{"content":"Done."}}]}

                            data: [DONE]

                            """,
                            Encoding.UTF8,
                            "text/event-stream")
                    });
            });

        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var events = await ReadAllAsync(
            client.GenerateAgentRoundStreamAsync(
                new LlmAgentRoundRequest("system", "user", [], []),
                CancellationToken.None));

        Assert.Equal(2, requestCount);
        Assert.Equal("Done.", Assert.Single(events).Content);
    }

    [Fact]
    public async Task GenerateAgentRoundStreamAsync_WhenRateLimitPersists_ExposesGroqDetails()
    {
        var requestCount = 0;
        using var handler = new StubHttpMessageHandler(
            (_, _) =>
            {
                requestCount++;
                var response = new HttpResponseMessage(
                    HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent(
                        """
                        {
                          "error": {
                            "message": "Rate limit reached for tokens per minute.",
                            "type": "tokens"
                          }
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
                response.Headers.TryAddWithoutValidation("retry-after", "0");
                response.Headers.TryAddWithoutValidation(
                    "x-request-id",
                    "req_test_123");
                response.Headers.TryAddWithoutValidation(
                    "x-ratelimit-limit-tokens",
                    "8000");
                response.Headers.TryAddWithoutValidation(
                    "x-ratelimit-remaining-tokens",
                    "0");
                response.Headers.TryAddWithoutValidation(
                    "x-ratelimit-reset-tokens",
                    "12s");
                return Task.FromResult(response);
            });

        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<GroqApiException>(
            async () => await ReadAllAsync(
                client.GenerateAgentRoundStreamAsync(
                    new LlmAgentRoundRequest(
                        "system",
                        "user",
                        [],
                        []),
                    CancellationToken.None)));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(6, requestCount);
        Assert.Equal("0", exception.RetryAfter);
        Assert.Equal("req_test_123", exception.RequestId);
        Assert.Equal(
            "8000",
            exception.ResponseHeaders["x-ratelimit-limit-tokens"]);
        Assert.Equal(
            "0",
            exception.ResponseHeaders["x-ratelimit-remaining-tokens"]);
        Assert.Contains("tokens per minute", exception.ResponseBody);
        Assert.Contains("x-ratelimit-reset-tokens=12s", exception.Message);
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
            }),
            NullLogger<GroqLlmClient>.Instance);
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
