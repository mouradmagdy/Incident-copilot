namespace IncidentCopilot.Application.Chat;

public sealed record ChatStreamResponse(
    IAsyncEnumerable<ChatStreamEvent> Events,
    IReadOnlyList<ChatSource> Sources);
