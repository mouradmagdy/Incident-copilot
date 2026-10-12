namespace IncidentCopilot.Application.Knowledge;

public sealed class KnowledgeOptions
{
    public const string SectionName = "Knowledge";

    public string RunbooksPath { get; set; } = string.Empty;

    public int TargetChunkSize { get; set; } = 800;

    public int ChunkOverlap { get; set; } = 100;
}
