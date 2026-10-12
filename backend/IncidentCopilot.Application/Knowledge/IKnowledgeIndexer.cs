namespace IncidentCopilot.Application.Knowledge;



public interface IKnowledgeIndexer
{
    Task IndexAllAsync(
        int? targetChunkSize = null,
        int? chunkOverlap = null,
        CancellationToken cancellationToken = default);
}
