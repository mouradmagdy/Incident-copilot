using System.Net.Http.Json;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.AI.Contracts;

namespace IncidentCopilot.Infrastructure.AI;

public sealed class FastApiEmbeddingClient : IEmbeddingClient
{
    public const string HttpClientName = "EmbeddingService";

    private readonly IHttpClientFactory _httpClientFactory;

    public FastApiEmbeddingClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var embeddings = await EmbedAsync([text], cancellationToken);

        return embeddings[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var request = new EmbeddingRequest(texts);

        using var response = await client.PostAsJsonAsync(
            "embed",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var embeddings = await response.Content
            .ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);

        if (embeddings is null)
        {
            throw new InvalidOperationException(
                "The embedding service returned an empty response body.");
        }

        if (embeddings.Count != texts.Count)
        {
            throw new InvalidOperationException(
                "The embedding service returned a different number of " +
                "embeddings than requested.");
        }

        return embeddings;
    }
}
