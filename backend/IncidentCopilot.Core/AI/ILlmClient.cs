namespace IncidentCopilot.Core.AI;

public interface ILlmClient
{
    Task<LlmResponse> GenerateAsync(
        LlmRequest request,
        CancellationToken cancellationToken);

    Task<LlmTurnResponse> GenerateWithToolsAsync(
        LlmToolRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<LlmStreamEvent> GenerateWithToolsStreamAsync(
        LlmToolRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<LlmStreamEvent> GenerateAgentRoundStreamAsync(
        LlmAgentRoundRequest request,
        CancellationToken cancellationToken);

    Task<LlmResponse> GenerateAfterToolAsync(
        LlmToolContinuationRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<string> GenerateAfterToolStreamAsync(
        LlmToolContinuationRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<string> GenerateStreamAsync(
        LlmRequest request,
        CancellationToken cancellationToken);
}
