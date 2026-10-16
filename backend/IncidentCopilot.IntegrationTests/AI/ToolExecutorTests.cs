using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.AI.Tools.CreateIncident;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task ExecuteAsync_ReadOnlyToolWithPendingActionId_IsRejectedBeforeExecution()
    {
        var tool = new CountingTool(ToolExecutionMode.ReadOnly);

        var result = await new ToolExecutor().ExecuteAsync(
            tool,
            new ToolExecutionContext("{}", Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("cannot execute as a write action", result.Error);
        Assert.Equal(0, tool.ExecutionCount);
    }

    [Fact]
    public async Task ExecuteAsync_CreateIncidentWithoutApproval_IsRejectedBeforeExecution()
    {
        await using var dbContext = CreateDbContext();
        var tool = new CreateIncidentTool(dbContext);

        var result = await new ToolExecutor().ExecuteAsync(
            tool,
            new ToolExecutionContext(
                "{\"serviceName\":\"OrderService\",\"title\":\"Issue\",\"severity\":\"High\"}"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("requires an approved pending action", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_CreateIncidentWithInvalidArguments_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var tool = new CreateIncidentTool(dbContext);

        var result = await new ToolExecutor().ExecuteAsync(
            tool,
            new ToolExecutionContext(
                "{\"serviceName\":\"OrderService\",\"title\":\"Issue\",\"severity\":\"Override\"}",
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Unknown severity", result.Error);
    }

    private static IncidentCopilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IncidentCopilotDbContext>()
            .UseNpgsql(
                "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused")
            .Options;

        return new IncidentCopilotDbContext(options);
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

    private sealed class CountingTool : IAgentTool
    {
        public CountingTool(ToolExecutionMode executionMode)
        {
            ExecutionMode = executionMode;
        }

        public string Name => "counting_tool";

        public ToolExecutionMode ExecutionMode { get; }

        public int ExecutionCount { get; private set; }

        public ToolDefinition Definition => new(
            Name,
            "Counts executions.",
            new { type = "object" });

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.FromResult(new ToolResult(true, "{}"));
        }
    }
}
