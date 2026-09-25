using IncidentCopilot.Core.Enums;

namespace IncidentCopilot.Infrastructure.Simulation;

public interface ISimulationScenarioState
{
    SimulationScenario Current { get; }

    void Set(SimulationScenario scenario);
}
