using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.HasKey(incident => incident.Id);

        builder.Property(incident => incident.Id)
            .ValueGeneratedOnAdd();

        builder.Property(incident => incident.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(incident => incident.Description)
            .HasMaxLength(4_000);

        builder.Property(incident => incident.StartedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(incident => incident.ResolvedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(incident => incident.RootCause)
            .HasMaxLength(4_000);

        builder.HasIndex(incident => incident.PendingActionId)
            .IsUnique();

        builder.HasOne<PendingAction>()
            .WithMany()
            .HasForeignKey(incident => incident.PendingActionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(incident => new { incident.Status, incident.StartedAtUtc })
            .IsDescending(false, true);
    }
}
