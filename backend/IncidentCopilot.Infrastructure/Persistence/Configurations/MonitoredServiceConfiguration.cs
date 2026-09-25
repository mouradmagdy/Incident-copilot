using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public class MonitoredServiceConfiguration : IEntityTypeConfiguration<MonitoredService>
{
    public void Configure(EntityTypeBuilder<MonitoredService> builder)
    {
        builder.HasKey(service => service.Id);

        builder.Property(service => service.Id)
            .ValueGeneratedOnAdd();

        builder.Property(service => service.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(service => service.Description)
            .HasMaxLength(1_000)
            .IsRequired();

        builder.HasIndex(service => service.Name)
            .IsUnique();
    }
}
