namespace IncidentCopilot.Application.AI.Tools;

public sealed class ToolExecutor
{
    private static readonly TimeSpan ToolTimeout =
        TimeSpan.FromSeconds(3);

    public async Task<ToolResult> ExecuteAsync(
        IAgentTool tool,
        string argumentsJson,
        CancellationToken requestCancellationToken)
    {
        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                requestCancellationToken);

        timeoutCts.CancelAfter(ToolTimeout);

        try
        {
            return await tool.ExecuteAsync(
                argumentsJson,
                timeoutCts.Token);
        }
        catch (OperationCanceledException)
            when (!requestCancellationToken.IsCancellationRequested)
        {
            return new ToolResult(
                false,
                string.Empty,
                $"Tool '{tool.Name}' timed out.");
        }
    }
}
