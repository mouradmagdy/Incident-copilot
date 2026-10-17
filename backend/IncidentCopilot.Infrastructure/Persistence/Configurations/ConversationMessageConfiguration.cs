using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public sealed class ConversationMessageConfiguration
    : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Role)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(message => message.Content)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(message => message.SourcesJson)
            .HasColumnType("jsonb");

        builder.Property(message => message.DebugJson)
            .HasColumnType("jsonb");

        builder.Property(message => message.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(message => message.Conversation)
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(message => message.PendingAction)
            .WithOne()
            .HasForeignKey<ConversationMessage>(message => message.PendingActionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(message => new
        {
            message.ConversationId,
            message.CreatedAtUtc,
            message.Id
        });

        builder.HasIndex(message => message.PendingActionId)
            .IsUnique();
    }
}
