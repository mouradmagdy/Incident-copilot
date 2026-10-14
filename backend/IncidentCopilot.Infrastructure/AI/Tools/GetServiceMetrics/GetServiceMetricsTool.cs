using System.Text.Json;
using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.AI.Tools.GetServiceMetrics;

public sealed class GetServiceMetricsTool : IAgentTool
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IncidentCopilotDbContext _dbContext;
    private readonly IToolFaultInjector _faultInjector;

    public GetServiceMetricsTool(
        IncidentCopilotDbContext dbContext,
        IToolFaultInjector faultInjector)
    {
        _dbContext = dbContext;
        _faultInjector = faultInjector;
    }

    public string Name => "get_service_metrics";

    public ToolDefinition Definition => new(
        Name,
        "Gets the latest runtime metrics for a monitored service. " +
        "Use this when the user asks about current service latency, " +
        "CPU, memory, errors, queue depth, request rate, or database connections.",
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
                    description = "How far back to consider recent telemetry.",
                    minimum = 1,
                    maximum = 60
                }
            },
            required = new[] { "serviceName" },
            additionalProperties = false
        });

    public async Task<ToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        if (_faultInjector.CurrentFault == ToolFault.MetricsTimeout)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                cancellationToken);
        }

        if (_faultInjector.CurrentFault == ToolFault.MetricsFailure)
        {
            return new ToolResult(
                false,
                string.Empty,
                $"Injected failure for tool '{Name}'.");
        }

        GetServiceMetricsArguments? arguments;

        try
        {
            arguments = JsonSerializer.Deserialize<GetServiceMetricsArguments>(
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
        var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

        var metric = await _dbContext.ServiceMetrics
            .AsNoTracking()
            .Where(metric =>
                metric.Service.Name == arguments.ServiceName &&
                metric.TimestampUtc >= cutoff)
            .OrderByDescending(metric => metric.TimestampUtc)
            .Select(metric => new ServiceMetricsToolResponse(
                metric.Service.Name,
                minutes,
                metric.TimestampUtc,
                metric.RequestsPerSecond,
                metric.AverageLatencyMs,
                metric.P95LatencyMs,
                metric.ErrorRatePercent,
                metric.CpuUsagePercent,
                metric.MemoryUsagePercent,
                metric.QueueDepth,
                metric.ActiveDbConnections))
            .FirstOrDefaultAsync(cancellationToken);

        if (metric is null)
        {
            return new ToolResult(
                false,
                string.Empty,
                $"No recent metrics were found for service '{arguments.ServiceName}'.");
        }

        return new ToolResult(
            true,
            JsonSerializer.Serialize(metric, SerializerOptions));
    }
}
