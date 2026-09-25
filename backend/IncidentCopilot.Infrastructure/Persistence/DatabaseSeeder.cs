using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        IncidentCopilotDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Services.AnyAsync(cancellationToken))
        {
            return;
        }

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

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}