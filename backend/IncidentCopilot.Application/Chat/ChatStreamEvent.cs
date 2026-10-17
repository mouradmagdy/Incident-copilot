using IncidentCopilot.Application.PendingActions;

namespace IncidentCopilot.Application.Chat;

public abstract record ChatStreamEvent;

public sealed record ChatDeltaEvent(string Content)
    : ChatStreamEvent;

public sealed record ChatPendingActionEvent(PendingActionView Action)
    : ChatStreamEvent;

public sealed record ChatDebugEvent(ChatDebugInfo Debug)
    : ChatStreamEvent;
