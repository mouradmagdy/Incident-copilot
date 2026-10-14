using IncidentCopilot.Api.Contracts.Simulation;
using IncidentCopilot.Application.AI.Tools;
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

        group.MapGet("/tool-fault", GetToolFault)
            .Produces<ToolFaultResponse>();

        group.MapPost("/tool-fault", SetToolFault)
            .Produces<ToolFaultResponse>()
            .ProducesValidationProblem();

        return endpoints;
    }

    private static IResult GetToolFault(IToolFaultState faultState)
    {
        return Results.Ok(new ToolFaultResponse(
            faultState.CurrentFault.ToString()));
    }

    private static IResult SetToolFault(
        SetToolFaultRequest request,
        IToolFaultState faultState)
    {
        if (!Enum.TryParse<ToolFault>(
                request.Fault,
                ignoreCase: true,
                out var fault)
            || !Enum.IsDefined(fault))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Fault)] =
                [
                    $"Unknown tool fault '{request.Fault}'. Valid faults: " +
                    string.Join(", ", Enum.GetNames<ToolFault>())
                ]
            });
        }

        faultState.Set(fault);

        return Results.Ok(new ToolFaultResponse(fault.ToString()));
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
