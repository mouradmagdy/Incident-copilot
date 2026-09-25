namespace IncidentCopilot.Api.Contracts.Services;

public sealed record ServiceResponse(
    int Id,
    string Name,
    string Description,
    bool IsActive);
