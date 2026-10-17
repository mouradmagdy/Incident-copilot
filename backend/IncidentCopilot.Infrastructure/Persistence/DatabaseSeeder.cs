using IncidentCopilot.Core.Entities;
using IncidentCopilot.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        IncidentCopilotDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Services.AnyAsync(cancellationToken))
        {
            var services = new[]
            {
                new MonitoredService
                {
                    Name = "OrderService",
                    Description = "Handles order creation and order lifecycle management."
                },
                new MonitoredService
                {
                    Name = "PaymentService",
                    Description = "Handles payment authorization and payment processing."
                },
                new MonitoredService
                {
                    Name = "ProcessingWorker",
                    Description = "Processes queued orders asynchronously."
                },
                new MonitoredService
                {
                    Name = "NotificationService",
                    Description = "Sends customer notifications."
                }
            };

            dbContext.Services.AddRange(services);
        }

        if (!await dbContext.Incidents.AnyAsync(cancellationToken))
        {
            dbContext.Incidents.AddRange(
                new Incident
                {
                    Title = "Payment authorization latency",
                    Description = "Elevated authorization latency is under investigation.",
                    Severity = IncidentSeverity.Medium,
                    Status = IncidentStatus.Investigating,
                    StartedAtUtc = DateTime.UtcNow.AddHours(-3)
                },
                new Incident
                {
                    Title = "Notification delivery delay",
                    Description = "A previous notification queue delay has been resolved.",
                    Severity = IncidentSeverity.Low,
                    Status = IncidentStatus.Resolved,
                    StartedAtUtc = DateTime.UtcNow.AddDays(-1),
                    ResolvedAtUtc = DateTime.UtcNow.AddHours(-18)
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
