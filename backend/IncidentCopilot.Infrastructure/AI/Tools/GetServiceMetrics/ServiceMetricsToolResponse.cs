namespace IncidentCopilot.Infrastructure.AI.Tools.GetServiceMetrics;

internal sealed record ServiceMetricsToolResponse(
    string ServiceName,
    int WindowMinutes,
    DateTime TimestampUtc,
    double RequestsPerSecond,
    double AverageLatencyMs,
    double P95LatencyMs,
    double ErrorRatePercent,
    double CpuUsagePercent,
    double MemoryUsagePercent,
    int QueueDepth,
    int ActiveDbConnections);
