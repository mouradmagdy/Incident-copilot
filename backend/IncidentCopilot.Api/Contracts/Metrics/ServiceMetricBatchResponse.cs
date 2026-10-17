namespace IncidentCopilot.Api.Contracts.Metrics;

public sealed record ServiceMetricBatchResponse(
    int ServiceId,
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
