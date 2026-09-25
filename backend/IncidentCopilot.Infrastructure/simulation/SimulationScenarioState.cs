using IncidentCopilot.Core.Enums;
using Microsoft.Extensions.Options;

namespace IncidentCopilot.Infrastructure.Simulation;

public class SimulationScenarioState : ISimulationScenarioState
{
    private int _current;

    public SimulationScenarioState(
        IOptions<TelemetrySimulationOptions> options)
    {
        _current = (int)options.Value.Scenario;
    }

    public SimulationScenario Current =>
        (SimulationScenario)Volatile.Read(ref _current);

    public void Set(SimulationScenario scenario)
    {
        Volatile.Write(ref _current, (int)scenario);
    }
}
