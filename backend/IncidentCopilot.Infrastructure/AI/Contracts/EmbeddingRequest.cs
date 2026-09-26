namespace IncidentCopilot.Infrastructure.AI.Contracts;

internal sealed class EmbeddingRequest : List<string>
{
    public EmbeddingRequest(IEnumerable<string> texts)
        : base(texts)
    {
    }
}
