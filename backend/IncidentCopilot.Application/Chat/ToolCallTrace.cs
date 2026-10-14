namespace IncidentCopilot.Application.Chat;

public sealed record ToolCallTrace(
    string ToolName,
    string ArgumentsJson,
    bool Success,
    string? Error);
