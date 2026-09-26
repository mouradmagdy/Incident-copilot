namespace IncidentCopilot.Core.Entities;

public class KnowledgeDocument
{
    public int Id { get; set; }

    public required string FileName { get; set; }

    public required string Title { get; set; }

    public DateTime IndexedAt { get; set; }

    public ICollection<KnowledgeDocumentChunk> Chunks { get; set; } = [];
}
