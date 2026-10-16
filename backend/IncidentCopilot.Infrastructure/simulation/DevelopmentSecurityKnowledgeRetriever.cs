using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.Knowledge;
using Microsoft.Extensions.Hosting;

namespace IncidentCopilot.Infrastructure.Simulation;

public sealed class DevelopmentSecurityKnowledgeRetriever
    : IKnowledgeRetriever
{
    private readonly IKnowledgeRetriever _inner;
    private readonly IToolFaultInjector _faultInjector;
    private readonly string _securityRunbookPath;

    public DevelopmentSecurityKnowledgeRetriever(
        IKnowledgeRetriever inner,
        IToolFaultInjector faultInjector,
        IHostEnvironment environment)
    {
        _inner = inner;
        _faultInjector = faultInjector;
        _securityRunbookPath = Path.GetFullPath(
            "../../docs/security-tests/prompt-injection.md",
            environment.ContentRootPath);
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var results = await _inner.SearchAsync(
            query,
            topK,
            cancellationToken);

        if (_faultInjector.CurrentFault !=
            ToolFault.RagPromptInjection)
        {
            return results;
        }

        var maliciousContent = await File.ReadAllTextAsync(
            _securityRunbookPath,
            cancellationToken);
        var injectedChunk = new RetrievedChunk(
            -1,
            -1,
            Path.GetFileName(_securityRunbookPath),
            "Malicious indirect prompt-injection fixture",
            maliciousContent,
            0);

        return results
            .Append(injectedChunk)
            .OrderBy(chunk => chunk.Distance)
            .Take(topK)
            .ToArray();
    }
}
