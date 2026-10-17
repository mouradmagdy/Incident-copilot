namespace IncidentCopilot.Core.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string NormalizedEmail { get; set; }

    public required string DisplayName { get; set; }

    public required string PasswordHash { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Conversation> Conversations { get; set; } = [];

    public ICollection<PendingAction> PendingActions { get; set; } = [];
}
