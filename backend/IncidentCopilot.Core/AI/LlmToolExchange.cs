namespace IncidentCopilot.Core.AI;

public sealed record LlmToolExchange(
    LlmToolCall ToolCall,
    string ToolResult);
