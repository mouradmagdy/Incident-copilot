namespace IncidentCopilot.Application.PendingActions;

public interface IPendingActionService
{
    Task<PendingActionView> CreateAsync(
        PendingActionRequest request,
        CancellationToken cancellationToken = default);

    Task<PendingActionView?> ConfirmAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PendingActionView?> CancelAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}
