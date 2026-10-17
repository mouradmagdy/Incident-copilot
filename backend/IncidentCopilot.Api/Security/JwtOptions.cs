namespace IncidentCopilot.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "IncidentCopilot";

    public string Audience { get; set; } = "IncidentCopilot.Web";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 480;
}
