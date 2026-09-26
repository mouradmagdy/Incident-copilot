using System.Net;
using System.Text;
using IncidentCopilot.Infrastructure.AI;

namespace IncidentCopilot.IntegrationTests.AI;

public sealed class FastApiEmbeddingClientTests
{
    [Fact]
    public async Task EmbedAsync_PostsTypedArrayAndReturnsVectors()
    {
        using var handler = new StubHttpMessageHandler(
            async (request, cancellationToken) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal(
                    "http://localhost:8000/embed",
                    request.RequestUri?.AbsoluteUri);
                Assert.Equal(
                    "[\"first\",\"second\"]",
                    await request.Content!.ReadAsStringAsync(cancellationToken));

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "[[0.1,0.2],[0.3,0.4]]",
                        Encoding.UTF8,
                        "application/json")
                };
            });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000/")
        };
        var factory = new StubHttpClientFactory(httpClient);
        var client = new FastApiEmbeddingClient(factory);

        var result = await client.EmbedAsync(
            ["first", "second"],
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal([0.1f, 0.2f], result[0]);
        Assert.Equal([0.3f, 0.4f], result[1]);
        Assert.Equal(
            FastApiEmbeddingClient.HttpClientName,
            factory.RequestedClientName);
    }

    [Fact]
    public async Task EmbedAsync_PropagatesCancellationToken()
    {
        using var handler = new StubHttpMessageHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000/")
        };
        var client = new FastApiEmbeddingClient(
            new StubHttpClientFactory(httpClient));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.EmbedAsync("text", cancellation.Token));
    }

    private sealed class StubHttpClientFactory(HttpClient client)
        : IHttpClientFactory
    {
        public string? RequestedClientName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            RequestedClientName = name;
            return client;
        }
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
