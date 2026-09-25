namespace IncidentCopilot.Core.AI;

public sealed record LlmRequest(
    string SystemPrompt,
    string UserMessage);
