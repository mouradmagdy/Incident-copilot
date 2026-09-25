using IncidentCopilot.Api.Contracts.Simulation;
using IncidentCopilot.Core.Enums;
using IncidentCopilot.Infrastructure.Simulation;

namespace IncidentCopilot.Api.Endpoints;

public static class SimulationEndpoints
{
    public static IEndpointRouteBuilder MapSimulationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/simulation")
            .WithTags("Simulation");

        group.MapGet("/scenario", GetScenario)
            .Produces<SimulationScenarioResponse>();

        group.MapPost("/scenario", SetScenario)
            .Produces<SimulationScenarioResponse>()
            .ProducesValidationProblem();

        return endpoints;
    }

    private static IResult GetScenario(
        ISimulationScenarioState scenarioState)
    {
        return Results.Ok(new SimulationScenarioResponse(
            scenarioState.Current.ToString()));
    }

    private static IResult SetScenario(
        SetSimulationScenarioRequest request,
        ISimulationScenarioState scenarioState)
    {
        if (!Enum.TryParse<SimulationScenario>(
                request.Scenario,
                ignoreCase: true,
                out var scenario)
            || !Enum.IsDefined(scenario))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Scenario)] =
                [
                    $"Unknown scenario '{request.Scenario}'. Valid scenarios: " +
                    string.Join(", ", Enum.GetNames<SimulationScenario>())
                ]
            });
        }

        scenarioState.Set(scenario);

        return Results.Ok(new SimulationScenarioResponse(
            scenario.ToString()));
    }
}
