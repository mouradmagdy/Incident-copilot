using System.Text;
using System.Text.Json;
using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.PendingActions;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Core.Entities;
using IncidentCopilot.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IncidentCopilot.Infrastructure.Persistence;

public sealed class PendingActionService : IPendingActionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IncidentCopilotDbContext _dbContext;
    private readonly ToolExecutor _toolExecutor;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<PendingActionService> _logger;
    private readonly IReadOnlyDictionary<string, IAgentTool> _tools;

    public PendingActionService(
        IncidentCopilotDbContext dbContext,
        ToolExecutor toolExecutor,
        ILlmClient llmClient,
        ILogger<PendingActionService> logger,
        IEnumerable<IAgentTool> tools)
    {
        _dbContext = dbContext;
        _toolExecutor = toolExecutor;
        _llmClient = llmClient;
        _logger = logger;
        _tools = tools.ToDictionary(
            tool => tool.Name,
            StringComparer.Ordinal);
    }

    public async Task<PendingActionView> CreateAsync(
        PendingActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var action = new PendingAction
        {
            Id = Guid.NewGuid(),
            ToolName = request.ToolName,
            ToolCallId = request.ToolCallId,
            ArgumentsJson = request.ArgumentsJson,
            Description = request.Description,
            SystemPrompt = request.SystemPrompt,
            UserMessage = request.UserMessage,
            ToolExchangesJson = JsonSerializer.Serialize(
                request.ToolExchanges,
                SerializerOptions),
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

        try
        {
            action.AssistantResponse =
                await GenerateAssistantResponseAsync(
                    action,
                    result,
                    cancellationToken);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Could not resume the agent after pending action {PendingActionId} completed.",
                action.Id);
        }

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
            action.Error,
            action.AssistantResponse);
    }

    private async Task<string?> GenerateAssistantResponseAsync(
        PendingAction action,
        ToolResult result,
        CancellationToken cancellationToken)
    {
        var exchanges = JsonSerializer
            .Deserialize<List<LlmToolExchange>>(
                action.ToolExchangesJson,
                SerializerOptions)
            ?? [];

        var serializedResult = result.Success
            ? result.Content
            : JsonSerializer.Serialize(new
            {
                error = result.Error ??
                    "The confirmed action failed without an error message."
            });

        exchanges.Add(new LlmToolExchange(
            new LlmToolCall(
                action.ToolCallId,
                action.ToolName,
                action.ArgumentsJson),
            serializedResult));

        var content = new StringBuilder();
        var continuationPrompt =
            $"""
            {action.SystemPrompt}

            The user explicitly confirmed the pending action. Its tool result
            is now included in the conversation. Give the user a concise final
            response grounded in that verified result. Clearly state whether
            the action succeeded or failed and include useful identifiers and
            status values returned by the tool. Do not request another tool and
            do not mention internal approval implementation details.
            """;

        await foreach (var streamEvent in _llmClient
            .GenerateAgentRoundStreamAsync(
                new LlmAgentRoundRequest(
                    continuationPrompt,
                    action.UserMessage,
                    [],
                    exchanges),
                cancellationToken)
            .WithCancellation(cancellationToken))
        {
            if (streamEvent.ToolCall is not null)
            {
                throw new InvalidOperationException(
                    "The resumed agent requested another tool instead of returning a final response.");
            }

            if (!string.IsNullOrEmpty(streamEvent.Content))
            {
                content.Append(streamEvent.Content);
            }
        }

        return content.Length == 0
            ? null
            : content.ToString();
    }
}
