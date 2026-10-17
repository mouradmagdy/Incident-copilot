using IncidentCopilot.Application.Chat;
using IncidentCopilot.Application.PendingActions;

namespace IncidentCopilot.Application.Conversations;

public sealed record ConversationSummary(
    Guid Id,
    string Title,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ConversationMessageView(
    long Id,
    string Role,
    string Content,
    DateTime CreatedAtUtc,
    IReadOnlyList<ChatSource> Sources,
    ChatDebugInfo? Debug,
    PendingActionView? PendingAction);

public sealed record ConversationView(
    Guid Id,
    string Title,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<ConversationMessageView> Messages);

public sealed record AddConversationMessageRequest(
    string Role,
    string Content,
    IReadOnlyList<ChatSource>? Sources = null,
    ChatDebugInfo? Debug = null,
    Guid? PendingActionId = null);
