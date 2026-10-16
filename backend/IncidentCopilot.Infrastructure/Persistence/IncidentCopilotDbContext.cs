using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.Persistence;

public class IncidentCopilotDbContext : DbContext
{
    public IncidentCopilotDbContext(
        DbContextOptions<IncidentCopilotDbContext> options)
        : base(options)
    {
    }

    public DbSet<MonitoredService> Services => Set<MonitoredService>();

    public DbSet<ServiceMetric> ServiceMetrics => Set<ServiceMetric>();

    public DbSet<LogEvent> LogEvents => Set<LogEvent>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<PendingAction> PendingActions => Set<PendingAction>();

    public DbSet<KnowledgeDocument> KnowledgeDocuments =>
        Set<KnowledgeDocument>();

    public DbSet<KnowledgeDocumentChunk> KnowledgeDocumentChunks =>
        Set<KnowledgeDocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IncidentCopilotDbContext).Assembly);
    }
}
