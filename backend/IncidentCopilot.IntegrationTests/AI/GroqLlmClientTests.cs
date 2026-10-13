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
