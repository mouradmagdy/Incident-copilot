using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.PendingActions;
using IncidentCopilot.Core.Entities;
using IncidentCopilot.Core.Enums;

namespace IncidentCopilot.IntegrationTests.PendingActions;

public sealed class PendingActionExecutionGateTests
{
    private static readonly DateTime UtcNow =
        new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_NonexistentAction_DoesNotExecute()
    {
        var executionCount = 0;
        var result = await new PendingActionExecutionGate().ExecuteAsync(
            null,
            UtcNow,
            (_, _) =>
            {
                executionCount++;
                return Task.FromResult(new ToolResult(true, "{}"));
            });

        Assert.Equal(PendingActionExecutionOutcome.NotFound, result.Outcome);
        Assert.Equal(0, executionCount);
    }

    [Fact]
    public async Task ExecuteAsync_ExpiredAction_DoesNotExecute()
    {
        var executionCount = 0;
        var action = CreateAction(
            PendingActionStatus.Pending,
            UtcNow - PendingActionExecutionGate.Lifetime);

        var result = await new PendingActionExecutionGate().ExecuteAsync(
            action,
            UtcNow,
            (_, _) =>
            {
                executionCount++;
                return Task.FromResult(new ToolResult(true, "{}"));
            });

        Assert.Equal(PendingActionExecutionOutcome.Expired, result.Outcome);
        Assert.Equal(PendingActionStatus.Expired, action.Status);
        Assert.Equal(UtcNow, action.DecidedAtUtc);
        Assert.Equal(0, executionCount);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledAction_DoesNotExecute()
    {
        var executionCount = 0;
        var action = CreateAction(
            PendingActionStatus.Cancelled,
            UtcNow.AddMinutes(-1));

        var result = await new PendingActionExecutionGate().ExecuteAsync(
            action,
            UtcNow,
            (_, _) =>
            {
                executionCount++;
                return Task.FromResult(new ToolResult(true, "{}"));
            });

        Assert.Equal(
            PendingActionExecutionOutcome.AlreadyDecided,
            result.Outcome);
        Assert.Equal(0, executionCount);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedAction_RemainsIdempotent()
    {
        var executionCount = 0;
        var action = CreateAction(
            PendingActionStatus.Completed,
            UtcNow.AddMinutes(-1));
        action.ResultJson = "{\"id\":42}";

        var first = await new PendingActionExecutionGate().ExecuteAsync(
            action,
            UtcNow,
            (_, _) =>
            {
                executionCount++;
                return Task.FromResult(new ToolResult(true, "{\"id\":43}"));
            });
        var second = await new PendingActionExecutionGate().ExecuteAsync(
            action,
            UtcNow.AddSeconds(1),
            (_, _) =>
            {
                executionCount++;
                return Task.FromResult(new ToolResult(true, "{\"id\":44}"));
            });

        Assert.Equal(
            PendingActionExecutionOutcome.AlreadyDecided,
            first.Outcome);
        Assert.Equal(
            PendingActionExecutionOutcome.AlreadyDecided,
            second.Outcome);
        Assert.Equal("{\"id\":42}", action.ResultJson);
        Assert.Equal(0, executionCount);
    }

    private static PendingAction CreateAction(
        PendingActionStatus status,
        DateTime createdAtUtc)
    {
        return new PendingAction
        {
            Id = Guid.NewGuid(),
            ToolName = "create_incident",
            ArgumentsJson = "{}",
            Description = "Create an incident.",
            ToolCallId = "call_incident",
            SystemPrompt = "system",
            UserMessage = "user",
            ToolExchangesJson = "[]",
            Status = status,
            CreatedAtUtc = createdAtUtc
        };
    }
}
