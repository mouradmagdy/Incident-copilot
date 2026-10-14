

namespace IncidentCopilot.Core.AI;

public sealed record LlmToolContinuationRequest(
    string SystemPrompt,
    string UserMessage,
    LlmToolCall ToolCall,
    string ToolResult);
