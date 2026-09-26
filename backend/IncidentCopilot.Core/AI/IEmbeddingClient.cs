namespace IncidentCopilot.Core.AI;

public interface IEmbeddingClient
{
    Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken);
}
