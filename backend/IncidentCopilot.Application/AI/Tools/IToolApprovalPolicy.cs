using IncidentCopilot.Core.AI;

namespace IncidentCopilot.Application.AI.Tools;

public interface IToolApprovalPolicy
{
    string ToolName { get; }

    ToolApprovalDecision Evaluate(
        string argumentsJson,
        IReadOnlyList<LlmToolExchange> toolExchanges);
}
