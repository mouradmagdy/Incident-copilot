namespace IncidentCopilot.Core.AI;

public sealed record LlmAgentRoundRequest(
    string SystemPrompt,
    string UserMessage,
    IReadOnlyList<ToolDefinition> Tools,
    IReadOnlyList<LlmToolExchange> ToolExchanges);
