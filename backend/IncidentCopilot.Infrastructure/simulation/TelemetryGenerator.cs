using IncidentCopilot.Core.Entities;
using IncidentCopilot.Core.Enums;
using LogEventLevel = IncidentCopilot.Core.Enums.LogLevel;

namespace IncidentCopilot.Infrastructure.Simulation;

public class TelemetryGenerator : ITelemetryGenerator
{
    private const string DatabasePressureServiceName = "OrderService";

    private static readonly string[] DatabasePressureMessages =
    [
        "Timeout while waiting for database connection.",
        "Database connection pool exhausted.",
        "Database operation exceeded timeout."
    ];

    public ServiceMetric GenerateMetric(
        MonitoredService service,
        DateTime timestampUtc,
        SimulationScenario scenario)
    {
        if (IsDatabaseConnectionExhaustion(service, scenario))
        {
            return GenerateDatabaseConnectionExhaustionMetric(
                service,
                timestampUtc);
        }

        var averageLatencyMs = Random.Shared.Next(150, 301);

        var p95LatencyMs =
            averageLatencyMs + Random.Shared.Next(80, 201);

        return new ServiceMetric
        {
            ServiceId = service.Id,

            TimestampUtc = timestampUtc,

            RequestsPerSecond =
                Random.Shared.Next(20, 51),

            AverageLatencyMs =
                averageLatencyMs,

            P95LatencyMs =
                p95LatencyMs,

            ErrorRatePercent =
                Random.Shared.NextDouble(),

            CpuUsagePercent =
                Random.Shared.Next(25, 51),

            MemoryUsagePercent =
                Random.Shared.Next(35, 61),

            QueueDepth =
                Random.Shared.Next(0, 31),

            ActiveDbConnections =
                Random.Shared.Next(15, 36)
        };
    }

    public LogEvent? GenerateLog(
        MonitoredService service,
        DateTime timestampUtc,
        SimulationScenario scenario)
    {
        if (!IsDatabaseConnectionExhaustion(service, scenario))
        {
            return null;
        }

        var message = DatabasePressureMessages[
            Random.Shared.Next(DatabasePressureMessages.Length)];

        return new LogEvent
        {
            ServiceId = service.Id,
            TimestampUtc = timestampUtc,
            Level = LogEventLevel.Error,
            Message = message,
            ExceptionType = message.Contains("pool", StringComparison.Ordinal)
                ? "NpgsqlException"
                : "TimeoutException",
            TraceId = Guid.NewGuid().ToString("N")
        };
    }

    private static ServiceMetric GenerateDatabaseConnectionExhaustionMetric(
        MonitoredService service,
        DateTime timestampUtc)
    {
        return new ServiceMetric
        {
            ServiceId = service.Id,
            TimestampUtc = timestampUtc,
            RequestsPerSecond = Random.Shared.Next(10, 31),
            AverageLatencyMs = Random.Shared.Next(1_500, 3_001),
            P95LatencyMs = Random.Shared.Next(3_000, 6_001),
            ErrorRatePercent = 8 + (Random.Shared.NextDouble() * 12),
            CpuUsagePercent = Random.Shared.Next(30, 56),
            MemoryUsagePercent = Random.Shared.Next(40, 66),
            QueueDepth = Random.Shared.Next(50, 151),
            ActiveDbConnections = Random.Shared.Next(95, 101)
        };
    }

    private static bool IsDatabaseConnectionExhaustion(
        MonitoredService service,
        SimulationScenario scenario)
    {
        return scenario == SimulationScenario.DatabaseConnectionExhaustion
            && string.Equals(
                service.Name,
                DatabasePressureServiceName,
                StringComparison.Ordinal);
    }
}
