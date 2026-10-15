using IncidentCopilot.Application.Observability;

namespace IncidentCopilot.IntegrationTests.Observability;

public sealed class IncidentCopilotTelemetryTests
{
    [Fact]
    public void Telemetry_UsesRegisteredNames()
    {
        Assert.Equal(
            "IncidentCopilot",
            IncidentCopilotTelemetry.ActivitySource.Name);
        Assert.Equal(
            "IncidentCopilot",
            IncidentCopilotTelemetry.Meter.Name);
        Assert.Equal(
            "incidentcopilot.agent.runs",
            IncidentCopilotTelemetry.AgentRuns.Name);
        Assert.Equal(
            "incidentcopilot.agent.duration",
            IncidentCopilotTelemetry.AgentDuration.Name);
        Assert.Equal(
            "incidentcopilot.tool.calls",
            IncidentCopilotTelemetry.ToolCalls.Name);
        Assert.Equal(
            "incidentcopilot.tool.duration",
            IncidentCopilotTelemetry.ToolDuration.Name);
    }
}
