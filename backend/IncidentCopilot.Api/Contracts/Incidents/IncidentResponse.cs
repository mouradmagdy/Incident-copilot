namespace IncidentCopilot.Api.Contracts.Incidents;

public sealed record IncidentResponse(
    int Id,
    string Title,
    string? Description,
    string Severity,
    string Status,
    DateTime StartedAtUtc,
    DateTime? ResolvedAtUtc,
    string? RootCause);

public sealed record UpdateIncidentStatusRequest(string Status);
