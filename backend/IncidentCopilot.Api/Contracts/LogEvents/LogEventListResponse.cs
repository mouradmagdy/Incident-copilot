using LogEventLevel = IncidentCopilot.Core.Enums.LogLevel;

namespace IncidentCopilot.Api.Contracts.LogEvents;

public sealed record LogEventListResponse(
    long Id,
    int ServiceId,
    string ServiceName,
    DateTime TimestampUtc,
    LogEventLevel Level,
    string Message,
    string? ExceptionType,
    string? TraceId);
