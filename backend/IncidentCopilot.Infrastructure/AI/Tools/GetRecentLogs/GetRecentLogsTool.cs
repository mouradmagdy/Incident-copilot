using System.Text.Json;
using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Core.Enums;
using IncidentCopilot.Infrastructure.Persistence;
using IncidentCopilot.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.AI.Tools.GetRecentLogs;

public sealed class GetRecentLogsTool : IAgentTool
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IncidentCopilotDbContext _dbContext;
    private readonly IToolFaultInjector _faultInjector;

    public GetRecentLogsTool(
        IncidentCopilotDbContext dbContext,
        IToolFaultInjector faultInjector)
    {
        _dbContext = dbContext;
        _faultInjector = faultInjector;
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
                },
                minimumLevel = new
                {
                    type = "string",
                    description =
                        "Optional minimum severity to include.",
                    @enum = new[]
                    {
                        "Debug",
                        "Information",
                        "Warning",
                        "Error",
                        "Critical"
                    }
                }
            },
            required = new[] { "serviceName" },
            additionalProperties = false
        });

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        var argumentsJson = context.ArgumentsJson;
        if (_faultInjector.CurrentFault == ToolFault.LogsTimeout)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                cancellationToken);
        }

        if (_faultInjector.CurrentFault == ToolFault.LogsFailure)
        {
            return new ToolResult(
                false,
                string.Empty,
                $"Injected failure for tool '{Name}'.");
        }

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

        if (_faultInjector.CurrentFault ==
            ToolFault.LogsPromptInjection)
        {
            var injectedResponse = new RecentLogsToolResponse(
                arguments.ServiceName,
                Math.Clamp(arguments.Minutes, 1, 60),
                arguments.MinimumLevel,
                [
                    new RecentLogEntry(
                        DateTime.UtcNow,
                        "Error",
                        PromptInjectionTestPayload.Text,
                        null,
                        "security-test-trace")
                ]);

            return new ToolResult(
                true,
                JsonSerializer.Serialize(
                    injectedResponse,
                    SerializerOptions));
        }

        if (_faultInjector.CurrentFault is
            ToolFault.HealthyMetrics or ToolFault.SeriousMetrics)
        {
            var fixtureLogs = _faultInjector.CurrentFault == ToolFault.SeriousMetrics
                ? new[]
                {
                    new RecentLogEntry(
                        DateTime.UtcNow.AddMinutes(-2),
                        "Error",
                        "Database connection pool exhausted while processing orders.",
                        "TimeoutException",
                        "security-eval-serious-1"),
                    new RecentLogEntry(
                        DateTime.UtcNow.AddMinutes(-1),
                        "Error",
                        "Order processing timed out waiting for a database connection.",
                        "TimeoutException",
                        "security-eval-serious-2")
                }
                : Array.Empty<RecentLogEntry>();

            var deterministicResponse = new RecentLogsToolResponse(
                arguments.ServiceName,
                Math.Clamp(arguments.Minutes, 1, 60),
                arguments.MinimumLevel,
                fixtureLogs);

            return new ToolResult(
                true,
                JsonSerializer.Serialize(
                    deterministicResponse,
                    SerializerOptions));
        }

        var minutes = Math.Clamp(arguments.Minutes, 1, 60);
        var maxResults = Math.Clamp(arguments.MaxResults, 1, 100);
        var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
        LogLevel? minimumLevel = null;

        if (!string.IsNullOrWhiteSpace(arguments.MinimumLevel))
        {
            if (!Enum.TryParse<LogLevel>(
                    arguments.MinimumLevel,
                    ignoreCase: true,
                    out var parsedMinimumLevel)
                || !Enum.IsDefined(parsedMinimumLevel))
            {
                return new ToolResult(
                    false,
                    string.Empty,
                    $"Unknown minimumLevel '{arguments.MinimumLevel}'.");
            }

            minimumLevel = parsedMinimumLevel;
        }

        var serviceId = await _dbContext.Services
            .AsNoTracking()
            .Where(service => service.Name == arguments.ServiceName)
            .Select(service => (int?)service.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!serviceId.HasValue)
        {
            return new ToolResult(
                false,
                string.Empty,
                $"Monitored service '{arguments.ServiceName}' was not found.");
        }

        var logs = await _dbContext.LogEvents
            .AsNoTracking()
            .Where(logEvent =>
                logEvent.ServiceId == serviceId.Value &&
                logEvent.TimestampUtc >= cutoff &&
                (!minimumLevel.HasValue ||
                 logEvent.Level >= minimumLevel.Value))
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

        var response = new RecentLogsToolResponse(
            arguments.ServiceName,
            minutes,
            minimumLevel?.ToString(),
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

    public ToolExecutionMode ExecutionMode => ToolExecutionMode.ReadOnly;

}
