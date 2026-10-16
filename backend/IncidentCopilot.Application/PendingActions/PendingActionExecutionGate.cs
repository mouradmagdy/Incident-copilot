using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Core.Entities;
using IncidentCopilot.Core.Enums;

namespace IncidentCopilot.Application.PendingActions;

public sealed class PendingActionExecutionGate
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public async Task<PendingActionExecutionResult> ExecuteAsync(
        PendingAction? action,
        DateTime utcNow,
        Func<PendingAction, CancellationToken, Task<ToolResult>> execute,
        CancellationToken cancellationToken = default)
    {
        if (action is null)
        {
            return new PendingActionExecutionResult(
                PendingActionExecutionOutcome.NotFound,
                null);
        }

        if (action.Status != PendingActionStatus.Pending)
        {
            return new PendingActionExecutionResult(
                PendingActionExecutionOutcome.AlreadyDecided,
                null);
        }

        if (action.CreatedAtUtc <= utcNow - Lifetime)
        {
            action.Status = PendingActionStatus.Expired;
            action.DecidedAtUtc = utcNow;
            action.Error = "The pending action expired before confirmation.";

            return new PendingActionExecutionResult(
                PendingActionExecutionOutcome.Expired,
                null);
        }

        var toolResult = await execute(action, cancellationToken);
        return new PendingActionExecutionResult(
            PendingActionExecutionOutcome.Executed,
            toolResult);
    }
}

public sealed record PendingActionExecutionResult(
    PendingActionExecutionOutcome Outcome,
    ToolResult? ToolResult);

public enum PendingActionExecutionOutcome
{
    NotFound,
    Expired,
    AlreadyDecided,
    Executed
}
