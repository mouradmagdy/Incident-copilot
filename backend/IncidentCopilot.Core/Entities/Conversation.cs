namespace IncidentCopilot.Core.Entities;

public sealed class Conversation
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public required string Title { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = [];
}
