using IncidentCopilot.Api.Contracts.Metrics;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController : ControllerBase
{
    private const int SamplesPerService = 500;
    private readonly IncidentCopilotDbContext _dbContext;

    public MetricsController(IncidentCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceMetricBatchResponse>>> Get(
        [FromQuery] int[] serviceIds,
        [FromQuery] string range = "24h",
        CancellationToken cancellationToken = default)
    {
        var ids = serviceIds
            .Where(id => id > 0)
            .Distinct()
            .Take(50)
            .ToArray();

        if (ids.Length == 0)
        {
            return Ok(Array.Empty<ServiceMetricBatchResponse>());
        }

        if (!TryGetRange(range, out var duration))
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(range)] =
                        ["Range must be one of: 1h, 6h, 24h, 7d."]
                }));
        }

        var fromUtc = DateTime.UtcNow - duration;
        var take = ids.Length * SamplesPerService;
        var metrics = await _dbContext.ServiceMetrics
            .AsNoTracking()
            .Where(metric =>
                ids.Contains(metric.ServiceId) &&
                metric.TimestampUtc >= fromUtc)
            .OrderByDescending(metric => metric.TimestampUtc)
            .Take(take)
            .Select(metric => new ServiceMetricBatchResponse(
                metric.ServiceId,
                metric.Id,
                metric.TimestampUtc,
                metric.RequestsPerSecond,
                metric.AverageLatencyMs,
                metric.P95LatencyMs,
                metric.ErrorRatePercent,
                metric.CpuUsagePercent,
                metric.MemoryUsagePercent,
                metric.QueueDepth,
                metric.ActiveDbConnections))
            .ToListAsync(cancellationToken);

        return Ok(metrics);
    }

    private static bool TryGetRange(string value, out TimeSpan duration)
    {
        duration = value switch
        {
            "1h" => TimeSpan.FromHours(1),
            "6h" => TimeSpan.FromHours(6),
            "24h" => TimeSpan.FromHours(24),
            "7d" => TimeSpan.FromDays(7),
            _ => TimeSpan.Zero
        };

        return duration > TimeSpan.Zero;
    }
}
