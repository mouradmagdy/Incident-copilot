using LogEventLevel = IncidentCopilot.Core.Enums.LogLevel;

namespace IncidentCopilot.Api.Contracts.Services;

public sealed record LogEventResponse(
    long Id,
    DateTime TimestampUtc,
    LogEventLevel Level,
    string Message,
    string? ExceptionType,
    string? TraceId);
