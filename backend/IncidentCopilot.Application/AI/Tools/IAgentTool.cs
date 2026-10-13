using IncidentCopilot.Core.AI;

namespace IncidentCopilot.Application.AI.Tools;

public interface IAgentTool
{
    string Name { get; }

    ToolDefinition Definition { get; }

    Task<ToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken);
}
