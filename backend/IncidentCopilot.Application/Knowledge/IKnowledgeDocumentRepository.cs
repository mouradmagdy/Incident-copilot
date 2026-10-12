using IncidentCopilot.Core.Entities;

namespace IncidentCopilot.Application.Knowledge;

public interface IKnowledgeDocumentRepository
{
    Task ReplaceAllAsync(
        IReadOnlyCollection<KnowledgeDocument> documents,
        CancellationToken cancellationToken);
}
