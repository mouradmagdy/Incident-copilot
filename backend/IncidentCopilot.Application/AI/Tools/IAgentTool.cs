using IncidentCopilot.Core.AI;

namespace IncidentCopilot.Application.AI.Tools;

public interface IAgentTool
{
    string Name { get; }
    ToolExecutionMode ExecutionMode { get; }

    ToolDefinition Definition { get; }
    string GetApprovalDescription(string argumentsJson) =>
        $"Execute {Name}.";

    Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken);
}
