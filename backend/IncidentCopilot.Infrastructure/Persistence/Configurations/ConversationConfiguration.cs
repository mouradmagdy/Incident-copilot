using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration
    : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Title)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(conversation => conversation.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(conversation => conversation.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(conversation => new
            {
                conversation.UserId,
                conversation.UpdatedAtUtc
            })
            .IsDescending(false, true);

        builder.HasOne(conversation => conversation.User)
            .WithMany(user => user.Conversations)
            .HasForeignKey(conversation => conversation.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
