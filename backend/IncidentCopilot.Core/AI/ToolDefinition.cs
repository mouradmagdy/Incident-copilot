namespace IncidentCopilot.Core.AI;

public sealed record ToolDefinition(
    string Name,
    string Description,
    object Parameters);
