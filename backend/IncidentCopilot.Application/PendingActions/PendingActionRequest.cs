using IncidentCopilot.Core.AI;

namespace IncidentCopilot.Application.PendingActions;

public sealed record PendingActionRequest(
    string ToolName,
    string ToolCallId,
    string ArgumentsJson,
    string Description,
    string SystemPrompt,
    string UserMessage,
    IReadOnlyList<LlmToolExchange> ToolExchanges,
    Guid? UserId = null);
