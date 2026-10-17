namespace IncidentCopilot.Application.Conversations;

public interface IConversationStore
{
    Task<ConversationView> CreateAsync(
        Guid userId,
        string firstMessage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummary>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ConversationView?> GetAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> AddMessageAsync(
        Guid userId,
        Guid conversationId,
        AddConversationMessageRequest request,
        CancellationToken cancellationToken = default);
}
