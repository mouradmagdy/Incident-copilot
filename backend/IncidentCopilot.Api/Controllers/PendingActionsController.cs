using IncidentCopilot.Api.Contracts.PendingActions;
using IncidentCopilot.Application.PendingActions;
using IncidentCopilot.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/pending-actions")]
public sealed class PendingActionsController : ControllerBase
{
    private readonly IPendingActionService _pendingActions;

    public PendingActionsController(
        IPendingActionService pendingActions)
    {
        _pendingActions = pendingActions;
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType<PendingActionResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PendingActionResponse>> Confirm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var action = await _pendingActions.ConfirmAsync(
            id,
            User.GetRequiredUserId(),
            cancellationToken);

        return action is null
            ? NotFound()
            : Ok(PendingActionResponse.From(action));
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<PendingActionResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PendingActionResponse>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var action = await _pendingActions.CancelAsync(
            id,
            User.GetRequiredUserId(),
            cancellationToken);

        return action is null
            ? NotFound()
            : Ok(PendingActionResponse.From(action));
    }
}
