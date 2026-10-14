namespace IncidentCopilot.Application.Chat;

public sealed record AgentRunResult(
    string Content,
    IReadOnlyList<ToolCallTrace> ToolCalls);
