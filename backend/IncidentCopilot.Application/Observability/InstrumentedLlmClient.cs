using IncidentCopilot.Core.AI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IncidentCopilot.Application.Observability;

public sealed class InstrumentedLlmClient : ILlmClient
{
    private readonly ILlmClient _inner;

    public InstrumentedLlmClient(ILlmClient inner)
    {
        _inner = inner;
    }

    public Task<LlmResponse> GenerateAsync(
        LlmRequest request,
        CancellationToken cancellationToken)
    {
        return MeasureAsync(
            "generate",
            streaming: false,
            token => _inner.GenerateAsync(request, token),
            cancellationToken);
    }

    public Task<LlmTurnResponse> GenerateWithToolsAsync(
        LlmToolRequest request,
        CancellationToken cancellationToken)
    {
        return MeasureAsync(
            "generate_with_tools",
            streaming: false,
            token => _inner.GenerateWithToolsAsync(request, token),
            cancellationToken);
    }

    public IAsyncEnumerable<LlmStreamEvent> GenerateWithToolsStreamAsync(
        LlmToolRequest request,
        CancellationToken cancellationToken)
    {
        return MeasureStreamAsync(
            "generate_with_tools",
            token => _inner.GenerateWithToolsStreamAsync(request, token),
            cancellationToken);
    }

    public IAsyncEnumerable<LlmStreamEvent> GenerateAgentRoundStreamAsync(
        LlmAgentRoundRequest request,
        CancellationToken cancellationToken)
    {
        return MeasureStreamAsync(
            "agent_round",
            token => _inner.GenerateAgentRoundStreamAsync(request, token),
            cancellationToken);
    }

    public Task<LlmResponse> GenerateAfterToolAsync(
        LlmToolContinuationRequest request,
        CancellationToken cancellationToken)
    {
        return MeasureAsync(
            "generate_after_tool",
            streaming: false,
            token => _inner.GenerateAfterToolAsync(request, token),
            cancellationToken);
    }

    public IAsyncEnumerable<string> GenerateAfterToolStreamAsync(
        LlmToolContinuationRequest request,
        CancellationToken cancellationToken)
    {
        return MeasureStreamAsync(
            "generate_after_tool",
            token => _inner.GenerateAfterToolStreamAsync(request, token),
            cancellationToken);
    }

    public IAsyncEnumerable<string> GenerateStreamAsync(
        LlmRequest request,
        CancellationToken cancellationToken)
    {
        return MeasureStreamAsync(
            "generate",
            token => _inner.GenerateStreamAsync(request, token),
            cancellationToken);
    }

    private static async Task<T> MeasureAsync<T>(
        string operation,
        bool streaming,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        using var activity = StartActivity(operation, streaming);
        var stopwatch = Stopwatch.StartNew();
        var outcome = "error";

        try
        {
            var result = await action(cancellationToken);
            outcome = "success";
            return result;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            outcome = "cancelled";
            throw;
        }
        finally
        {
            Complete(activity, stopwatch, operation, outcome);
        }
    }

    private static async IAsyncEnumerable<T> MeasureStreamAsync<T>(
        string operation,
        Func<CancellationToken, IAsyncEnumerable<T>> streamFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = StartActivity(operation, streaming: true);
        var stopwatch = Stopwatch.StartNew();
        var outcome = "error";

        try
        {
            await using var enumerator = streamFactory(cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                bool hasNext;

                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    outcome = "cancelled";
                    throw;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return enumerator.Current;
            }

            outcome = "success";
        }
        finally
        {
            Complete(activity, stopwatch, operation, outcome);
        }
    }

    private static Activity? StartActivity(
        string operation,
        bool streaming)
    {
        var activity = IncidentCopilotTelemetry.ActivitySource.StartActivity(
            "incidentcopilot.llm.call",
            ActivityKind.Internal);
        activity?.SetTag("llm.operation", operation);
        activity?.SetTag("llm.streaming", streaming);
        return activity;
    }

    private static void Complete(
        Activity? activity,
        Stopwatch stopwatch,
        string operation,
        string outcome)
    {
        stopwatch.Stop();
        activity?.SetTag("llm.outcome", outcome);

        if (outcome != "success")
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }

        var tags = new TagList
        {
            { "llm.operation", operation },
            { "llm.outcome", outcome }
        };

        IncidentCopilotTelemetry.LlmCalls.Add(1, tags);
        IncidentCopilotTelemetry.LlmDuration.Record(
            stopwatch.Elapsed.TotalSeconds,
            tags);
    }
}
