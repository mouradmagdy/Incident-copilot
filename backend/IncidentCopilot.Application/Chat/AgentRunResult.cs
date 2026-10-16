namespace IncidentCopilot.Application.Chat;

using IncidentCopilot.Application.PendingActions;

public sealed record AgentRunResult(
    string Content,
    IReadOnlyList<ToolCallTrace> ToolCalls,
    PendingActionView? PendingAction = null);
