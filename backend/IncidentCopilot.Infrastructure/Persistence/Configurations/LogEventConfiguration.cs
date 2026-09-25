using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public class LogEventConfiguration : IEntityTypeConfiguration<LogEvent>
{
    public void Configure(EntityTypeBuilder<LogEvent> builder)
    {
        builder.HasKey(logEvent => logEvent.Id);

        builder.Property(logEvent => logEvent.Id)
            .ValueGeneratedOnAdd();

        builder.Property(logEvent => logEvent.TimestampUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(logEvent => logEvent.Message)
            .HasMaxLength(4_000)
            .IsRequired();

        builder.Property(logEvent => logEvent.ExceptionType)
            .HasMaxLength(500);

        builder.Property(logEvent => logEvent.TraceId)
            .HasMaxLength(100);

        builder.HasIndex(logEvent => new { logEvent.ServiceId, logEvent.TimestampUtc })
            .IsDescending(false, true);

        builder.HasOne(logEvent => logEvent.Service)
            .WithMany(service => service.Logs)
            .HasForeignKey(logEvent => logEvent.ServiceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
