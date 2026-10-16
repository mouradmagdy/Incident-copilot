using IncidentCopilot.Application.PendingActions;

namespace IncidentCopilot.Api.Contracts.PendingActions;

public sealed record PendingActionResponse(
    Guid Id,
    string ToolName,
    string Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc,
    string? ResultJson,
    string? Error,
    string? AssistantResponse)
{
    public static PendingActionResponse From(PendingActionView action)
    {
        return new PendingActionResponse(
            action.Id,
            action.ToolName,
            action.Description,
            action.Status,
            action.CreatedAtUtc,
            action.DecidedAtUtc,
            action.ResultJson,
            action.Error,
            action.AssistantResponse);
    }
}
