namespace IncidentCopilot.Api.Contracts.Auth;

public sealed record RegisterRequest(
    string DisplayName,
    string Email,
    string Password);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record AuthUserResponse(
    Guid Id,
    string DisplayName,
    string Email);
