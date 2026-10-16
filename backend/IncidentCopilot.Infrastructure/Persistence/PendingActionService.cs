using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.PendingActions;
using IncidentCopilot.Core.Entities;
using IncidentCopilot.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.Persistence;

public sealed class PendingActionService : IPendingActionService
{
    private readonly IncidentCopilotDbContext _dbContext;
    private readonly ToolExecutor _toolExecutor;
    private readonly IReadOnlyDictionary<string, IAgentTool> _tools;

    public PendingActionService(
        IncidentCopilotDbContext dbContext,
        ToolExecutor toolExecutor,
        IEnumerable<IAgentTool> tools)
    {
        _dbContext = dbContext;
        _toolExecutor = toolExecutor;
        _tools = tools.ToDictionary(
            tool => tool.Name,
            StringComparer.Ordinal);
    }

    public async Task<PendingActionView> CreateAsync(
        string toolName,
        string argumentsJson,
        string description,
        CancellationToken cancellationToken = default)
    {
        var action = new PendingAction
        {
            Id = Guid.NewGuid(),
            ToolName = toolName,
            ArgumentsJson = argumentsJson,
            Description = description,
            Status = PendingActionStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.PendingActions.Add(action);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(action);
    }

    public async Task<PendingActionView?> ConfirmAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var action = await FindForUpdateAsync(id, cancellationToken);

        if (action is null)
        {
            return null;
        }

        if (action.Status != PendingActionStatus.Pending)
        {
            await transaction.CommitAsync(cancellationToken);
            return Map(action);
        }

        if (!_tools.TryGetValue(action.ToolName, out var tool) ||
            tool.ExecutionMode != ToolExecutionMode.RequiresApproval)
        {
            action.Status = PendingActionStatus.Failed;
            action.Error =
                "The requested approval tool is unavailable.";
            action.DecidedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(action);
        }

        ToolResult result;

        try
        {
            result = await _toolExecutor.ExecuteAsync(
                tool,
                new ToolExecutionContext(
                    action.ArgumentsJson,
                    action.Id),
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            result = new ToolResult(
                false,
                string.Empty,
                exception.Message);
        }

        action.Status = result.Success
            ? PendingActionStatus.Completed
            : PendingActionStatus.Failed;
        action.ResultJson = result.Success ? result.Content : null;
        action.Error = result.Error;
        action.DecidedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Map(action);
    }

    public async Task<PendingActionView?> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var action = await FindForUpdateAsync(id, cancellationToken);

        if (action is null)
        {
            return null;
        }

        if (action.Status == PendingActionStatus.Pending)
        {
            action.Status = PendingActionStatus.Cancelled;
            action.DecidedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return Map(action);
    }

    private Task<PendingAction?> FindForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.PendingActions
            .FromSqlInterpolated(
                $"""
                 SELECT *
                 FROM "PendingActions"
                 WHERE "Id" = {id}
                 FOR UPDATE
                 """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static PendingActionView Map(PendingAction action)
    {
        return new PendingActionView(
            action.Id,
            action.ToolName,
            action.Description,
            action.Status.ToString(),
            action.CreatedAtUtc,
            action.DecidedAtUtc,
            action.ResultJson,
            action.Error);
    }
}
