using IncidentCopilot.Api.Contracts.Services;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly IncidentCopilotDbContext _dbContext;

    public ServicesController(IncidentCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceResponse>>> GetServices(
        CancellationToken cancellationToken)
    {
        var services = await _dbContext.Services
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ServiceResponse(
                x.Id,
                x.Name,
                x.Description,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(services);
    }

    [HttpGet("{id:int}/metrics")]
    public async Task<ActionResult<List<ServiceMetricResponse>>> GetMetrics(
        int id,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Services
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!exists)
        {
            return NotFound();
        }

        var query = _dbContext.ServiceMetrics
            .AsNoTracking()
            .Where(x => x.ServiceId == id);

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.TimestampUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.TimestampUtc <= toUtc.Value);
        }

        var metrics = await query
            .OrderByDescending(x => x.TimestampUtc)
            .Take(500)
            .Select(x => new ServiceMetricResponse(
                x.Id,
                x.TimestampUtc,
                x.RequestsPerSecond,
                x.AverageLatencyMs,
                x.P95LatencyMs,
                x.ErrorRatePercent,
                x.CpuUsagePercent,
                x.MemoryUsagePercent,
                x.QueueDepth,
                x.ActiveDbConnections))
            .ToListAsync(cancellationToken);

        return Ok(metrics);
    }

    [HttpGet("{id:int}/logs")]
    public async Task<ActionResult<List<LogEventResponse>>> GetLogs(
        int id,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Services
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!exists)
        {
            return NotFound();
        }

        var query = _dbContext.LogEvents
            .AsNoTracking()
            .Where(x => x.ServiceId == id);

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.TimestampUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.TimestampUtc <= toUtc.Value);
        }

        var logs = await query
            .OrderByDescending(x => x.TimestampUtc)
            .Take(500)
            .Select(x => new LogEventResponse(
                x.Id,
                x.TimestampUtc,
                x.Level,
                x.Message,
                x.ExceptionType,
                x.TraceId))
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }
}
