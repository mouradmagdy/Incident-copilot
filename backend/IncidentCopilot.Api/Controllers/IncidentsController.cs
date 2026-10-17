using IncidentCopilot.Api.Contracts.Incidents;
using IncidentCopilot.Core.Enums;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public sealed class IncidentsController : ControllerBase
{
    private readonly IncidentCopilotDbContext _dbContext;

    public IncidentsController(IncidentCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IncidentResponse>>> Get(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var incidents = await _dbContext.Incidents
            .AsNoTracking()
            .OrderByDescending(incident => incident.StartedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(incident => new IncidentResponse(
                incident.Id,
                incident.Title,
                incident.Description,
                incident.Severity.ToString(),
                incident.Status.ToString(),
                incident.StartedAtUtc,
                incident.ResolvedAtUtc,
                incident.RootCause))
            .ToListAsync(cancellationToken);

        return Ok(incidents);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<IncidentResponse>> UpdateStatus(
        int id,
        UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IncidentStatus>(
                request.Status,
                ignoreCase: true,
                out var status) ||
            !Enum.IsDefined(status))
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Status)] =
                        ["Status must be Open, Investigating, or Resolved."]
                }));
        }

        var incident = await _dbContext.Incidents.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);

        if (incident is null)
        {
            return NotFound();
        }

        incident.Status = status;
        incident.ResolvedAtUtc = status == IncidentStatus.Resolved
            ? DateTime.UtcNow
            : null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new IncidentResponse(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Severity.ToString(),
            incident.Status.ToString(),
            incident.StartedAtUtc,
            incident.ResolvedAtUtc,
            incident.RootCause));
    }
}
