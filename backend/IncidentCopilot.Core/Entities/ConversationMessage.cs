namespace IncidentCopilot.Core.Entities;

public sealed class ConversationMessage
{
    public long Id { get; set; }

    public Guid ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public required string Role { get; set; }

    public required string Content { get; set; }

    public string? SourcesJson { get; set; }

    public string? DebugJson { get; set; }

    public Guid? PendingActionId { get; set; }

    public PendingAction? PendingAction { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
