namespace IncidentCopilot.Application.AI.Tools;

public sealed record ToolApprovalDecision(
    bool Allowed,
    string Reason);
