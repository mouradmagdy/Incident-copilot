namespace IncidentCopilot.Infrastructure.AI;

public sealed class EmbeddingServiceOptions
{
    public const string SectionName = "EmbeddingService";

    public string BaseUrl { get; set; } = "http://localhost:8000";
}
