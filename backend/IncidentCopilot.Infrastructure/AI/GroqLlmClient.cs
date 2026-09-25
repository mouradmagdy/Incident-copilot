using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IncidentCopilot.Core.AI;
using Microsoft.Extensions.Options;

namespace IncidentCopilot.Infrastructure.AI;

public sealed class GroqLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;

    public GroqLlmClient(
        HttpClient httpClient,
        IOptions<GroqOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(
            $"{_options.BaseUrl.TrimEnd('/')}/",
            UriKind.Absolute);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<LlmResponse> GenerateAsync(
        LlmRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new ChatCompletionRequest(
            _options.Model,
            [
                new ChatMessage("system", request.SystemPrompt),
                new ChatMessage("user", request.UserMessage)
            ]);

        using var response = await _httpClient.PostAsJsonAsync(
            "chat/completions",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var completion = await response.Content
            .ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);

        var content = completion?.Choices.FirstOrDefault()?.Message.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Groq returned a response without message content.");
        }

        return new LlmResponse(content);
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoice> Choices);

    private sealed record ChatCompletionChoice(
        [property: JsonPropertyName("message")] ChatMessage Message);
}
