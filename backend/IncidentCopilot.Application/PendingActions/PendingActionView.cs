namespace IncidentCopilot.Application.PendingActions;

public sealed record PendingActionView(
    Guid Id,
    string ToolName,
    string Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc,
    string? ResultJson,
    string? Error,
    string? AssistantResponse);
