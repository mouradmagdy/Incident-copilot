using Pgvector;

namespace IncidentCopilot.Core.Entities;

public class KnowledgeDocumentChunk
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public KnowledgeDocument Document { get; set; } = null!;

    public int ChunkIndex { get; set; }

    public required string Content { get; set; }

    public string? Section { get; set; }

    public Vector? Embedding { get; set; }
}
