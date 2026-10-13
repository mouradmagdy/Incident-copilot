namespace IncidentCopilot.Application.Chat;

public sealed record ChatStreamResponse(
    IAsyncEnumerable<string> Content,
    IReadOnlyList<ChatSource> Sources);
