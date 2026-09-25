using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IncidentCopilot.Infrastructure.Simulation;

public class TelemetrySimulationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelemetryGenerator _telemetryGenerator;
    private readonly ILogger<TelemetrySimulationService> _logger;
    private readonly TimeSpan _interval;
    private readonly ISimulationScenarioState _scenarioState;

    public TelemetrySimulationService(
        IServiceScopeFactory scopeFactory,
        ITelemetryGenerator telemetryGenerator,
        ILogger<TelemetrySimulationService> logger,
        IOptions<TelemetrySimulationOptions> options,
        ISimulationScenarioState scenarioState)
    {
        _scopeFactory = scopeFactory;
        _telemetryGenerator = telemetryGenerator;
        _logger = logger;
        _interval = options.Value.Interval;
        _scenarioState = scenarioState;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        try
        {
            do
            {
                try
                {
                    await GenerateTelemetryAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An error occurred while generating telemetry.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }

    private async Task GenerateTelemetryAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var timestampUtc = DateTime.UtcNow;
        var scenario = _scenarioState.Current;

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IncidentCopilotDbContext>();

        var services = await dbContext.Services
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var service in services)
        {
            var metric =
                _telemetryGenerator.GenerateMetric(
                    service,
                    timestampUtc,
                    scenario);

            dbContext.ServiceMetrics.Add(metric);

            var logEvent = _telemetryGenerator.GenerateLog(
                service,
                timestampUtc,
                scenario);

            if (logEvent is not null)
            {
                dbContext.LogEvents.Add(logEvent);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated telemetry for {ServiceCount} services at {Timestamp}",
            services.Count,
            timestampUtc);
    }
}
