namespace IncidentCopilot.Core.AI;

public sealed record LlmToolRequest(
    string SystemPrompt,
    string UserMessage,
    IReadOnlyList<ToolDefinition> Tools);
