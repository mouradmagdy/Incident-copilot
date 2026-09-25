using IncidentCopilot.Core.Entities;
using IncidentCopilot.Core.Enums;

namespace IncidentCopilot.Infrastructure.Simulation;

public interface ITelemetryGenerator
{
    ServiceMetric GenerateMetric(
        MonitoredService service,
        DateTime timestampUtc,
        SimulationScenario scenario);

    LogEvent? GenerateLog(
        MonitoredService service,
        DateTime timestampUtc,
        SimulationScenario scenario);
}
