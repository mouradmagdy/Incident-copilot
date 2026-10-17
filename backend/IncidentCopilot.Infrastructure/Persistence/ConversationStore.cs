using System.Text.Json;
using IncidentCopilot.Application.Chat;
using IncidentCopilot.Application.Conversations;
using IncidentCopilot.Application.PendingActions;
using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.Persistence;

public sealed class ConversationStore : IConversationStore
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IncidentCopilotDbContext _dbContext;

    public ConversationStore(IncidentCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConversationView> CreateAsync(
        Guid userId,
        string firstMessage,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = BuildTitle(firstMessage),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ConversationView(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc,
            []);
    }

    public async Task<IReadOnlyList<ConversationSummary>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.UpdatedAtUtc)
            .Select(conversation => new ConversationSummary(
                conversation.Id,
                conversation.Title,
                conversation.CreatedAtUtc,
                conversation.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationView?> GetAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .Include(item => item.Messages)
                .ThenInclude(message => message.PendingAction)
            .SingleOrDefaultAsync(
                item => item.Id == id && item.UserId == userId,
                cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var messages = conversation.Messages
            .OrderBy(message => message.CreatedAtUtc)
            .ThenBy(message => message.Id)
            .Select(MapMessage)
            .ToArray();

        return new ConversationView(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc,
            messages);
    }

    public async Task<bool> AddMessageAsync(
        Guid userId,
        Guid conversationId,
        AddConversationMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .SingleOrDefaultAsync(
                item => item.Id == conversationId &&
                    item.UserId == userId,
                cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        conversation.UpdatedAtUtc = now;
        _dbContext.ConversationMessages.Add(new ConversationMessage
        {
            ConversationId = conversationId,
            Role = request.Role,
            Content = request.Content,
            SourcesJson = SerializeOrNull(request.Sources),
            DebugJson = SerializeOrNull(request.Debug),
            PendingActionId = request.PendingActionId,
            CreatedAtUtc = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ConversationMessageView MapMessage(
        ConversationMessage message)
    {
        var pendingAction = message.PendingAction is null
            ? null
            : MapPendingAction(message.PendingAction);
        var debug = DeserializeOrDefault<ChatDebugInfo?>(
            message.DebugJson,
            null);

        if (debug is not null && pendingAction is not null)
        {
            debug = debug with
            {
                PendingActionState = pendingAction.Status
            };
        }

        return new ConversationMessageView(
            message.Id,
            message.Role,
            message.Content,
            message.CreatedAtUtc,
            DeserializeOrDefault<IReadOnlyList<ChatSource>>(
                message.SourcesJson,
                []),
            debug,
            pendingAction);
    }

    private static PendingActionView MapPendingAction(PendingAction action)
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

    private static string BuildTitle(string firstMessage)
    {
        var normalized = string.Join(
            ' ',
            firstMessage.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        return normalized.Length <= 120
            ? normalized
            : $"{normalized[..117]}...";
    }

    private static string? SerializeOrNull<T>(T? value)
    {
        return value is null
            ? null
            : JsonSerializer.Serialize(value, SerializerOptions);
    }

    private static T DeserializeOrDefault<T>(
        string? json,
        T defaultValue)
    {
        return string.IsNullOrWhiteSpace(json)
            ? defaultValue
            : JsonSerializer.Deserialize<T>(json, SerializerOptions)
                ?? defaultValue;
    }
}
