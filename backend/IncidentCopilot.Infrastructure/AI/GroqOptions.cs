namespace IncidentCopilot.Infrastructure.AI;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";

    public string Model { get; set; } = "openai/gpt-oss-120b";
}
