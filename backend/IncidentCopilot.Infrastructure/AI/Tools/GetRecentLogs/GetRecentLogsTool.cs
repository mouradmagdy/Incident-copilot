using System.Text.Json;
using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.AI.Tools.GetRecentLogs;

public sealed class GetRecentLogsTool : IAgentTool
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IncidentCopilotDbContext _dbContext;

    public GetRecentLogsTool(IncidentCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string Name => "get_recent_logs";

    public ToolDefinition Definition => new(
        Name,
        "Gets recent runtime log events for a monitored service. " +
        "Use this when the user asks about current or recent logs, errors, " +
        "exceptions, warnings, trace IDs, or log messages.",
        new
        {
            type = "object",
            properties = new
            {
                serviceName = new
                {
                    type = "string",
                    description =
                        "Exact monitored service name, for example OrderService."
                },
                minutes = new
                {
                    type = "integer",
                    description = "How far back to search for log events.",
                    minimum = 1,
                    maximum = 60,
                    @default = 15
                },
                maxResults = new
                {
                    type = "integer",
                    description = "Maximum number of newest log events to return.",
                    minimum = 1,
                    maximum = 100,
                    @default = 20
                }
            },
            required = new[] { "serviceName" },
            additionalProperties = false
        });

    public async Task<ToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        GetRecentLogsArguments? arguments;

        try
        {
            arguments = JsonSerializer.Deserialize<GetRecentLogsArguments>(
                argumentsJson,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return new ToolResult(
                false,
                string.Empty,
                "Tool arguments were not valid JSON.");
        }

        if (arguments is null ||
            string.IsNullOrWhiteSpace(arguments.ServiceName))
        {
            return new ToolResult(
                false,
                string.Empty,
                "serviceName is required.");
        }

        var minutes = Math.Clamp(arguments.Minutes, 1, 60);
        var maxResults = Math.Clamp(arguments.MaxResults, 1, 100);
        var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

        var logs = await _dbContext.LogEvents
            .AsNoTracking()
            .Where(logEvent =>
                logEvent.Service.Name == arguments.ServiceName &&
                logEvent.TimestampUtc >= cutoff)
            .OrderByDescending(logEvent => logEvent.TimestampUtc)
            .Take(maxResults)
            .Select(logEvent => new
            {
                logEvent.TimestampUtc,
                logEvent.Level,
                logEvent.Message,
                logEvent.ExceptionType,
                logEvent.TraceId
            })
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
        {
            return new ToolResult(
                false,
                string.Empty,
                $"No logs were found for service '{arguments.ServiceName}' " +
                $"in the last {minutes} minute(s).");
        }

        var response = new RecentLogsToolResponse(
            arguments.ServiceName,
            minutes,
            logs.Select(logEvent => new RecentLogEntry(
                    logEvent.TimestampUtc,
                    logEvent.Level.ToString(),
                    logEvent.Message,
                    logEvent.ExceptionType,
                    logEvent.TraceId))
                .ToList());

        return new ToolResult(
            true,
            JsonSerializer.Serialize(response, SerializerOptions));
    }
}
