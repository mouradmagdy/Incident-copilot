using IncidentCopilot.Core.Enums;

namespace IncidentCopilot.Infrastructure.Simulation;

public class TelemetrySimulationOptions
{
    public const string SectionName = "TelemetrySimulation";

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);

    public SimulationScenario Scenario { get; set; } = SimulationScenario.Healthy;
}
