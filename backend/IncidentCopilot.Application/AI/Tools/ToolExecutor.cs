using System.Diagnostics;
using IncidentCopilot.Application.Observability;

namespace IncidentCopilot.Application.AI.Tools;

public sealed class ToolExecutor
{
    private static readonly TimeSpan ToolTimeout =
        TimeSpan.FromSeconds(3);

    public async Task<ToolResult> ExecuteAsync(
        IAgentTool tool,
        ToolExecutionContext context,
        CancellationToken requestCancellationToken)
    {
        var argumentsJson = context.ArgumentsJson;

        using var activity = IncidentCopilotTelemetry.ActivitySource
            .StartActivity(
                "incidentcopilot.tool.execute",
                ActivityKind.Internal);
        activity?.SetTag("tool.name", tool.Name);

        var stopwatch = Stopwatch.StartNew();
        var outcome = "error";

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                requestCancellationToken);

        timeoutCts.CancelAfter(ToolTimeout);

        try
        {
            var result = await tool.ExecuteAsync(
                context,
                timeoutCts.Token);
            outcome = result.Success ? "success" : "failure";
            return result;
        }
        catch (OperationCanceledException)
            when (!requestCancellationToken.IsCancellationRequested)
        {
            outcome = "timeout";
            return new ToolResult(
                false,
                string.Empty,
                $"Tool '{tool.Name}' timed out.");
        }
        catch (OperationCanceledException)
        {
            outcome = "cancelled";
            throw;
        }
        finally
        {
            stopwatch.Stop();
            activity?.SetTag("tool.outcome", outcome);

            if (outcome != "success")
            {
                activity?.SetStatus(ActivityStatusCode.Error);
            }

            var tags = new TagList
            {
                { "tool.name", tool.Name },
                { "tool.outcome", outcome }
            };

            IncidentCopilotTelemetry.ToolCalls.Add(1, tags);
            IncidentCopilotTelemetry.ToolDuration.Record(
                stopwatch.Elapsed.TotalSeconds,
                tags);
        }
    }
}
