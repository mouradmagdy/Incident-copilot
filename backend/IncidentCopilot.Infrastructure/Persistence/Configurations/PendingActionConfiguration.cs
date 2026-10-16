using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public sealed class PendingActionConfiguration
    : IEntityTypeConfiguration<PendingAction>
{
    public void Configure(EntityTypeBuilder<PendingAction> builder)
    {
        builder.HasKey(action => action.Id);

        builder.Property(action => action.ToolName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(action => action.ArgumentsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(action => action.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(action => action.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(action => action.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(action => action.DecidedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(action => action.ResultJson)
            .HasColumnType("jsonb");

        builder.Property(action => action.Error)
            .HasMaxLength(2_000);

        builder.HasIndex(action => new
        {
            action.Status,
            action.CreatedAtUtc
        });
    }
}
