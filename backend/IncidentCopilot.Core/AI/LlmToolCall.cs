namespace IncidentCopilot.Core.AI;

public sealed record LlmToolCall(
    string Id,
    string Name,
    string ArgumentsJson);
