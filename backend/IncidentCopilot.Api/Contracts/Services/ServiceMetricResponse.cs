namespace IncidentCopilot.Api.Contracts.Services;

public sealed record ServiceMetricResponse(
    long Id,
    DateTime TimestampUtc,
    double RequestsPerSecond,
    double AverageLatencyMs,
    double P95LatencyMs,
    double ErrorRatePercent,
    double CpuUsagePercent,
    double MemoryUsagePercent,
    int QueueDepth,
    int ActiveDbConnections);
