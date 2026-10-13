namespace IncidentCopilot.Core.AI;

public sealed record LlmTurnResponse(
    string? Content,
    IReadOnlyList<LlmToolCall> ToolCalls);
