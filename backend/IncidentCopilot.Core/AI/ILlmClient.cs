namespace IncidentCopilot.Core.AI;

public interface ILlmClient
{
    Task<LlmResponse> GenerateAsync(
        LlmRequest request,
        CancellationToken cancellationToken);
}
