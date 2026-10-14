namespace IncidentCopilot.Core.AI;

public sealed record LlmStreamEvent(
    string? Content,
    LlmToolCall? ToolCall);
