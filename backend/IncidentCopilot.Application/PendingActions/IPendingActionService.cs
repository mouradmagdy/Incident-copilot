namespace IncidentCopilot.Application.PendingActions;

public interface IPendingActionService
{
    Task<PendingActionView> CreateAsync(
        string toolName,
        string argumentsJson,
        string description,
        CancellationToken cancellationToken = default);

    Task<PendingActionView?> ConfirmAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PendingActionView?> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
