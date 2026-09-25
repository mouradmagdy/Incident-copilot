using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public class ServiceMetricConfiguration : IEntityTypeConfiguration<ServiceMetric>
{
    public void Configure(EntityTypeBuilder<ServiceMetric> builder)
    {
        builder.HasKey(metric => metric.Id);

        builder.Property(metric => metric.Id)
            .ValueGeneratedOnAdd();

        builder.Property(metric => metric.TimestampUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(metric => new { metric.ServiceId, metric.TimestampUtc })
            .IsDescending(false, true);

        builder.HasOne(metric => metric.Service)
            .WithMany(service => service.Metrics)
            .HasForeignKey(metric => metric.ServiceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
