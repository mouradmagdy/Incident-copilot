using IncidentCopilot.Api.Contracts.LogEvents;
using IncidentCopilot.Core.Entities;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidentLogLevel = IncidentCopilot.Core.Enums.LogLevel;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/logs")]
public sealed class LogsController : ControllerBase
{
    private readonly IncidentCopilotDbContext _dbContext;

    public LogsController(IncidentCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<LogEventPageResponse>> Get(
        [FromQuery] int[]? serviceIds,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string range = "24h",
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreateFilter(serviceIds, range, level, search, out var filter, out var problem))
        {
            return BadRequest(problem);
        }

        var safePage = Math.Clamp(page, 1, 1_000_000);
        var safePageSize = Math.Clamp(pageSize, 10, 100);
        var query = ApplyFilter(_dbContext.LogEvents.AsNoTracking(), filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var logs = await query
            .OrderByDescending(log => log.TimestampUtc)
            .ThenByDescending(log => log.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(log => new LogEventListResponse(
                log.Id,
                log.ServiceId,
                log.Service.Name,
                log.TimestampUtc,
                log.Level,
                log.Message,
                log.ExceptionType,
                log.TraceId))
            .ToListAsync(cancellationToken);

        return Ok(new LogEventPageResponse(logs, totalCount, safePage, safePageSize));
    }

    [HttpGet("volume")]
    public async Task<ActionResult<IReadOnlyList<LogVolumeBucketResponse>>> GetVolume(
        [FromQuery] int[]? serviceIds,
        [FromQuery] string range = "24h",
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreateFilter(serviceIds, range, level, search, out var filter, out var problem))
        {
            return BadRequest(problem);
        }

        var query = ApplyFilter(_dbContext.LogEvents.AsNoTracking(), filter);
        var buckets = range switch
        {
            "7d" => await GetDailyVolume(query, cancellationToken),
            "24h" => await GetHourlyVolume(query, cancellationToken),
            "6h" => await GetMinuteVolume(query, 15, cancellationToken),
            _ => await GetMinuteVolume(query, 5, cancellationToken)
        };

        return Ok(buckets);
    }

    private static IQueryable<LogEvent> ApplyFilter(IQueryable<LogEvent> query, LogFilter filter)
    {
        query = query.Where(log => log.TimestampUtc >= filter.FromUtc);

        if (filter.ServiceIds.Length > 0)
        {
            query = query.Where(log => filter.ServiceIds.Contains(log.ServiceId));
        }

        if (filter.Level.HasValue)
        {
            query = query.Where(log => log.Level == filter.Level.Value);
        }

        if (filter.SearchPattern is not null)
        {
            query = query.Where(log =>
                EF.Functions.ILike(log.Message, filter.SearchPattern) ||
                (log.ExceptionType != null && EF.Functions.ILike(log.ExceptionType, filter.SearchPattern)) ||
                (log.TraceId != null && EF.Functions.ILike(log.TraceId, filter.SearchPattern)));
        }

        return query;
    }

    private static bool TryCreateFilter(
        int[]? serviceIds,
        string range,
        string? level,
        string? search,
        out LogFilter filter,
        out ValidationProblemDetails? problem)
    {
        filter = default!;
        problem = null;

        if (!TryGetRange(range, out var duration))
        {
            problem = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(range)] = ["Range must be one of: 1h, 6h, 24h, 7d."]
            });
            return false;
        }

        IncidentLogLevel? parsedLevel = null;
        if (!string.IsNullOrWhiteSpace(level))
        {
            if (!Enum.TryParse<IncidentLogLevel>(level, true, out var value))
            {
                problem = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [nameof(level)] = ["Level must be Debug, Information, Warning, Error, or Critical."]
                });
                return false;
            }

            parsedLevel = value;
        }

        var ids = serviceIds?
            .Where(id => id > 0)
            .Distinct()
            .Take(50)
            .ToArray() ?? [];
        var searchTerm = search?.Trim();
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm[..Math.Min(searchTerm.Length, 200)];
        }

        filter = new LogFilter(
            ids,
            DateTime.UtcNow - duration,
            parsedLevel,
            string.IsNullOrEmpty(searchTerm) ? null : $"%{searchTerm}%");
        return true;
    }

    private static async Task<IReadOnlyList<LogVolumeBucketResponse>> GetDailyVolume(
        IQueryable<LogEvent> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .GroupBy(log => new { log.TimestampUtc.Year, log.TimestampUtc.Month, log.TimestampUtc.Day })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Day,
                InfoCount = group.Count(log => log.Level == IncidentLogLevel.Debug || log.Level == IncidentLogLevel.Information),
                WarningCount = group.Count(log => log.Level == IncidentLogLevel.Warning),
                ErrorCount = group.Count(log => log.Level == IncidentLogLevel.Error || log.Level == IncidentLogLevel.Critical)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new LogVolumeBucketResponse(
                new DateTime(row.Year, row.Month, row.Day, 0, 0, 0, DateTimeKind.Utc),
                row.InfoCount,
                row.WarningCount,
                row.ErrorCount))
            .OrderBy(row => row.BucketStartUtc)
            .ToList();
    }

    private static async Task<IReadOnlyList<LogVolumeBucketResponse>> GetHourlyVolume(
        IQueryable<LogEvent> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .GroupBy(log => new { log.TimestampUtc.Year, log.TimestampUtc.Month, log.TimestampUtc.Day, log.TimestampUtc.Hour })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Day,
                group.Key.Hour,
                InfoCount = group.Count(log => log.Level == IncidentLogLevel.Debug || log.Level == IncidentLogLevel.Information),
                WarningCount = group.Count(log => log.Level == IncidentLogLevel.Warning),
                ErrorCount = group.Count(log => log.Level == IncidentLogLevel.Error || log.Level == IncidentLogLevel.Critical)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new LogVolumeBucketResponse(
                new DateTime(row.Year, row.Month, row.Day, row.Hour, 0, 0, DateTimeKind.Utc),
                row.InfoCount,
                row.WarningCount,
                row.ErrorCount))
            .OrderBy(row => row.BucketStartUtc)
            .ToList();
    }

    private static async Task<IReadOnlyList<LogVolumeBucketResponse>> GetMinuteVolume(
        IQueryable<LogEvent> query,
        int bucketMinutes,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .GroupBy(log => new
            {
                log.TimestampUtc.Year,
                log.TimestampUtc.Month,
                log.TimestampUtc.Day,
                log.TimestampUtc.Hour,
                Minute = log.TimestampUtc.Minute / bucketMinutes * bucketMinutes
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Day,
                group.Key.Hour,
                group.Key.Minute,
                InfoCount = group.Count(log => log.Level == IncidentLogLevel.Debug || log.Level == IncidentLogLevel.Information),
                WarningCount = group.Count(log => log.Level == IncidentLogLevel.Warning),
                ErrorCount = group.Count(log => log.Level == IncidentLogLevel.Error || log.Level == IncidentLogLevel.Critical)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new LogVolumeBucketResponse(
                new DateTime(row.Year, row.Month, row.Day, row.Hour, row.Minute, 0, DateTimeKind.Utc),
                row.InfoCount,
                row.WarningCount,
                row.ErrorCount))
            .OrderBy(row => row.BucketStartUtc)
            .ToList();
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

    private sealed record LogFilter(
        int[] ServiceIds,
        DateTime FromUtc,
        IncidentLogLevel? Level,
        string? SearchPattern);
}
