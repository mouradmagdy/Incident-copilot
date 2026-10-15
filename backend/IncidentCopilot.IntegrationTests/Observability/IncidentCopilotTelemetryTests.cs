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
            "incidentcopilot.llm.calls",
            IncidentCopilotTelemetry.LlmCalls.Name);
        Assert.Equal(
            "incidentcopilot.llm.duration",
            IncidentCopilotTelemetry.LlmDuration.Name);
        Assert.Equal(
            "incidentcopilot.retrieval.calls",
            IncidentCopilotTelemetry.RetrievalCalls.Name);
        Assert.Equal(
            "incidentcopilot.retrieval.duration",
            IncidentCopilotTelemetry.RetrievalDuration.Name);
        Assert.Equal(
            "incidentcopilot.tool.calls",
            IncidentCopilotTelemetry.ToolCalls.Name);
        Assert.Equal(
            "incidentcopilot.tool.duration",
            IncidentCopilotTelemetry.ToolDuration.Name);
    }
}
