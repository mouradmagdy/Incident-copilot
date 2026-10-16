using System.Net;

namespace IncidentCopilot.Infrastructure.AI;

public sealed class GroqApiException : HttpRequestException
{
    public GroqApiException(
        HttpStatusCode statusCode,
        string responseBody,
        IReadOnlyDictionary<string, string> responseHeaders)
        : base(
            CreateMessage(statusCode, responseHeaders),
            inner: null,
            statusCode)
    {
        ResponseBody = responseBody;
        ResponseHeaders = responseHeaders;
    }

    public string ResponseBody { get; }

    public IReadOnlyDictionary<string, string> ResponseHeaders { get; }

    public string? RetryAfter => GetHeader("retry-after");

    public string? RequestId => GetHeader("x-request-id");

    private string? GetHeader(string name)
    {
        return ResponseHeaders.TryGetValue(name, out var value)
            ? value
            : null;
    }

    private static string CreateMessage(
        HttpStatusCode statusCode,
        IReadOnlyDictionary<string, string> responseHeaders)
    {
        var headerSummary = responseHeaders.Count == 0
            ? "none"
            : string.Join(
                ", ",
                responseHeaders.Select(pair => $"{pair.Key}={pair.Value}"));

        return $"Groq API request failed with status " +
            $"{(int)statusCode} ({statusCode}). " +
            $"Headers: {headerSummary}. Response body omitted from logs.";
    }
}
