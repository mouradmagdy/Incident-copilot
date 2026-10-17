namespace IncidentCopilot.Api.Contracts.LogEvents;

public sealed record LogEventPageResponse(
    IReadOnlyList<LogEventListResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
