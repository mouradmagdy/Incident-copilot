using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Core.AI;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.Persistence;

public sealed class KnowledgeRetriever : IKnowledgeRetriever
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IncidentCopilotDbContext _dbContext;

    public KnowledgeRetriever(
        IEmbeddingClient embeddingClient,
        IncidentCopilotDbContext dbContext)
    {
        _embeddingClient = embeddingClient;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingClient.EmbedAsync(
            query,
            cancellationToken);
        var queryVector = new Vector(embedding);

        var results = await _dbContext.KnowledgeDocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null)
            .Select(chunk => new
            {
                chunk.Id,
                chunk.DocumentId,
                chunk.Content,
                chunk.Section,
                FileName = chunk.Document.FileName,
                Distance = chunk.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(chunk => chunk.Distance)
            .Take(topK)
            .ToListAsync(cancellationToken);

        return results
            .Select(chunk => new RetrievedChunk(
                chunk.Id,
                chunk.DocumentId,
                chunk.FileName,
                chunk.Section,
                chunk.Content,
                chunk.Distance))
            .ToList();
    }
}
