using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
            ],
            false);

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

    public async Task<LlmTurnResponse> GenerateWithToolsAsync(
        LlmToolRequest request,
        CancellationToken cancellationToken)
    {
        var tools = request.Tools
            .Select(tool => new ChatTool(
                "function",
                new ChatFunctionDefinition(
                    tool.Name,
                    tool.Description,
                    tool.Parameters)))
            .ToArray();

        var payload = new ChatCompletionRequest(
            _options.Model,
            [
                new ChatMessage("system", request.SystemPrompt),
                new ChatMessage("user", request.UserMessage)
            ],
            false,
            tools,
            "auto",
            false);

        using var response = await _httpClient.PostAsJsonAsync(
            "chat/completions",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var completion = await response.Content
            .ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);

        var message = completion?.Choices.FirstOrDefault()?.Message
            ?? throw new InvalidOperationException(
                "Groq returned a response without a message.");

        var toolCalls = message.ToolCalls?
            .Select(toolCall => new LlmToolCall(
                toolCall.Id,
                toolCall.Function.Name,
                toolCall.Function.Arguments))
            .ToArray()
            ?? [];

        return new LlmTurnResponse(message.Content, toolCalls);
    }

    public async IAsyncEnumerable<LlmStreamEvent> GenerateWithToolsStreamAsync(
        LlmToolRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tools = request.Tools
            .Select(tool => new ChatTool(
                "function",
                new ChatFunctionDefinition(
                    tool.Name,
                    tool.Description,
                    tool.Parameters)))
            .ToArray();

        var payload = new ChatCompletionRequest(
            _options.Model,
            [
                new ChatMessage("system", request.SystemPrompt),
                new ChatMessage("user", request.UserMessage)
            ],
            true,
            tools,
            "auto",
            false);

        string? toolCallId = null;
        string? toolName = null;
        var toolArguments = new StringBuilder();
        var hasToolCall = false;

        await foreach (var chunk in StreamCompletionAsync(
            payload,
            cancellationToken))
        {
            var delta = chunk.Choices.FirstOrDefault()?.Delta;
            if (!string.IsNullOrEmpty(delta?.Content))
            {
                yield return new LlmStreamEvent(delta.Content, null);
            }

            foreach (var toolCallDelta in delta?.ToolCalls ?? [])
            {
                if (toolCallDelta.Index != 0)
                {
                    throw new InvalidOperationException(
                        "Groq returned multiple streamed tool calls, but only one is supported.");
                }

                hasToolCall = true;
                toolCallId ??= toolCallDelta.Id;
                toolName ??= toolCallDelta.Function?.Name;

                if (!string.IsNullOrEmpty(toolCallDelta.Function?.Arguments))
                {
                    toolArguments.Append(toolCallDelta.Function.Arguments);
                }
            }
        }

        if (!hasToolCall)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(toolCallId) ||
            string.IsNullOrWhiteSpace(toolName))
        {
            throw new InvalidOperationException(
                "Groq returned an incomplete streamed tool call.");
        }

        yield return new LlmStreamEvent(
            null,
            new LlmToolCall(
                toolCallId,
                toolName,
                toolArguments.ToString()));
    }

    public async Task<LlmResponse> GenerateAfterToolAsync(
        LlmToolContinuationRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new ChatCompletionRequest(
            _options.Model,
            CreateToolContinuationMessages(request),
            false);

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
                "Groq returned a response without message content after tool execution.");
        }

        return new LlmResponse(content);
    }

    public async IAsyncEnumerable<string> GenerateAfterToolStreamAsync(
        LlmToolContinuationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var payload = new ChatCompletionRequest(
            _options.Model,
            CreateToolContinuationMessages(request),
            true);
        var hasContent = false;

        await foreach (var chunk in StreamCompletionAsync(
            payload,
            cancellationToken))
        {
            var content = chunk.Choices.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                hasContent = true;
                yield return content;
            }
        }

        if (!hasContent)
        {
            throw new InvalidOperationException(
                "Groq returned a streamed response without message content after tool execution.");
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        LlmRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var payload = new ChatCompletionRequest(
            _options.Model,
            [
                new ChatMessage("system", request.SystemPrompt),
                new ChatMessage("user", request.UserMessage)
            ],
            true);

        await foreach (var chunk in StreamCompletionAsync(
            payload,
            cancellationToken))
        {
            var content = chunk.Choices.FirstOrDefault()?.Delta?.Content;

            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    private async IAsyncEnumerable<ChatCompletionChunk> StreamCompletionAsync(
        ChatCompletionRequest payload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "chat/completions")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line["data: ".Length..];

            if (data == "[DONE]")
            {
                yield break;
            }

            var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data);
            if (chunk is not null)
            {
                yield return chunk;
            }
        }
    }

    private static IReadOnlyList<ChatMessage> CreateToolContinuationMessages(
        LlmToolContinuationRequest request)
    {
        var toolCall = new ChatToolCall(
            request.ToolCall.Id,
            "function",
            new ChatToolCallFunction(
                request.ToolCall.Name,
                request.ToolCall.ArgumentsJson));

        return
        [
            new ChatMessage("system", request.SystemPrompt),
            new ChatMessage("user", request.UserMessage),
            new ChatMessage("assistant", null, [toolCall]),
            new ChatMessage(
                "tool",
                request.ToolResult,
                ToolCallId: request.ToolCall.Id,
                Name: request.ToolCall.Name)
        ];
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("tools")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<ChatTool>? Tools = null,
        [property: JsonPropertyName("tool_choice")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? ToolChoice = null,
        [property: JsonPropertyName("parallel_tool_calls")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        bool? ParallelToolCalls = null);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("tool_calls")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<ChatToolCall>? ToolCalls = null,
        [property: JsonPropertyName("tool_call_id")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? ToolCallId = null,
        [property: JsonPropertyName("name")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? Name = null);

    private sealed record ChatTool(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("function")] ChatFunctionDefinition Function);

    private sealed record ChatFunctionDefinition(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("parameters")] object Parameters);

    private sealed record ChatToolCall(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("function")] ChatToolCallFunction Function);

    private sealed record ChatToolCallFunction(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("arguments")] string Arguments);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoice> Choices);

    private sealed record ChatCompletionChoice(
        [property: JsonPropertyName("message")] ChatMessage Message);

    private sealed record ChatCompletionChunk(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChunkChoice> Choices);

    private sealed record ChatCompletionChunkChoice(
        [property: JsonPropertyName("delta")] ChatCompletionDelta? Delta);

    private sealed record ChatCompletionDelta(
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("tool_calls")]
        IReadOnlyList<ChatToolCallDelta>? ToolCalls);

    private sealed record ChatToolCallDelta(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("function")]
        ChatToolCallFunctionDelta? Function);

    private sealed record ChatToolCallFunctionDelta(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("arguments")] string? Arguments);
}
