namespace IncidentCopilot.Infrastructure.AI.Tools.GetRecentLogs;

internal sealed record RecentLogsToolResponse(
    string ServiceName,
    int WindowMinutes,
    IReadOnlyList<RecentLogEntry> Logs);

internal sealed record RecentLogEntry(
    DateTime TimestampUtc,
    string Level,
    string Message,
    string? ExceptionType,
    string? TraceId);
