using IncidentCopilot.Application.Knowledge;
using System.Diagnostics;

namespace IncidentCopilot.Application.Observability;

public sealed class InstrumentedKnowledgeRetriever : IKnowledgeRetriever
{
    private readonly IKnowledgeRetriever _inner;

    public InstrumentedKnowledgeRetriever(IKnowledgeRetriever inner)
    {
        _inner = inner;
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        using var activity = IncidentCopilotTelemetry.ActivitySource
            .StartActivity(
                "incidentcopilot.retrieval.search",
                ActivityKind.Internal);
        activity?.SetTag("retrieval.top_k", topK);

        var stopwatch = Stopwatch.StartNew();
        var outcome = "error";

        try
        {
            var results = await _inner.SearchAsync(
                query,
                topK,
                cancellationToken);
            outcome = "success";
            activity?.SetTag("retrieval.result_count", results.Count);
            return results;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            outcome = "cancelled";
            throw;
        }
        finally
        {
            stopwatch.Stop();
            activity?.SetTag("retrieval.outcome", outcome);

            if (outcome != "success")
            {
                activity?.SetStatus(ActivityStatusCode.Error);
            }

            var tags = new TagList
            {
                { "retrieval.outcome", outcome }
            };

            IncidentCopilotTelemetry.RetrievalCalls.Add(1, tags);
            IncidentCopilotTelemetry.RetrievalDuration.Record(
                stopwatch.Elapsed.TotalSeconds,
                tags);
        }
    }
}
