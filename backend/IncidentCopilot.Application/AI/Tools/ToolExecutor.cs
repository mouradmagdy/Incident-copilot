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
            if (tool.ExecutionMode == ToolExecutionMode.RequiresApproval &&
                context.PendingActionId is null)
            {
                outcome = "approval_required";
                return new ToolResult(
                    false,
                    string.Empty,
                    $"Tool '{tool.Name}' requires an approved pending action.");
            }

            if (tool.ExecutionMode == ToolExecutionMode.ReadOnly &&
                context.PendingActionId is not null)
            {
                outcome = "invalid_execution_mode";
                return new ToolResult(
                    false,
                    string.Empty,
                    $"Read-only tool '{tool.Name}' cannot execute as a write action.");
            }

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

            if (tool.ExecutionMode == ToolExecutionMode.RequiresApproval &&
                outcome != "success")
            {
                IncidentCopilotTelemetry.WriteToolFailures.Add(
                    1,
                    new KeyValuePair<string, object?>(
                        "tool.name",
                        tool.Name),
                    new KeyValuePair<string, object?>(
                        "tool.outcome",
                        outcome));
            }
        }
    }
}
