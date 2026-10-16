using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Core.AI;

namespace IncidentCopilot.IntegrationTests.AI;

public sealed class ToolExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenToolExceedsTimeout_ReturnsFailure()
    {
        var executor = new ToolExecutor();
        var result = await executor.ExecuteAsync(
            new DelayingTool(),
            new ToolExecutionContext("{}"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(result.Content);
        Assert.Equal("Tool 'slow_tool' timed out.", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsCancelled_PropagatesCancellation()
    {
        var executor = new ToolExecutor();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => executor.ExecuteAsync(
                new DelayingTool(),
                new ToolExecutionContext("{}"),
                cancellation.Token));
    }

    private sealed class DelayingTool : IAgentTool
    {
        public string Name => "slow_tool";

        public ToolExecutionMode ExecutionMode =>
            ToolExecutionMode.ReadOnly;

        public ToolDefinition Definition => new(
            Name,
            "A tool that waits until cancelled.",
            new { type = "object" });

        public async Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new ToolResult(true, "{}");
        }
    }
}
