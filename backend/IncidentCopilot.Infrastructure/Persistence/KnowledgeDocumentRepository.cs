using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.Persistence;

public sealed class KnowledgeDocumentRepository
    : IKnowledgeDocumentRepository
{
    private readonly IncidentCopilotDbContext _dbContext;

    public KnowledgeDocumentRepository(IncidentCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ReplaceAllAsync(
        IReadOnlyCollection<KnowledgeDocument> documents,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        await _dbContext.KnowledgeDocuments
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.KnowledgeDocuments.AddRangeAsync(
            documents,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
