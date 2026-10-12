namespace IncidentCopilot.Application.Knowledge;



public interface IKnowledgeIndexer
{
     Task IndexAllAsync(CancellationToken cancellationToken = default);
}
