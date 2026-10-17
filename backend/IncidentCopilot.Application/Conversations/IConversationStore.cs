namespace IncidentCopilot.Application.Conversations;

public interface IConversationStore
{
    Task<ConversationView> CreateAsync(
        string firstMessage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummary>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<ConversationView?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> AddMessageAsync(
        Guid conversationId,
        AddConversationMessageRequest request,
        CancellationToken cancellationToken = default);
}
