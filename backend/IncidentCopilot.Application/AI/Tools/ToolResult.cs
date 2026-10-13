namespace IncidentCopilot.Application.AI.Tools;

public sealed record ToolResult(
    bool Success,
    string Content,
    string? Error = null);
