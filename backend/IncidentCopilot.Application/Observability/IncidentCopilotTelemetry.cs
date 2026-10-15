using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace IncidentCopilot.Application.Observability;

public static class IncidentCopilotTelemetry
{
    public const string Name = "IncidentCopilot";

    public static readonly ActivitySource ActivitySource = new(Name);

    public static readonly Meter Meter = new(Name);

    public static readonly Counter<long> AgentRuns = Meter.CreateCounter<long>(
        "incidentcopilot.agent.runs",
        unit: "{run}",
        description: "Number of completed incident-copilot agent runs.");

    public static readonly Histogram<double> AgentDuration =
        Meter.CreateHistogram<double>(
            "incidentcopilot.agent.duration",
            unit: "s",
            description: "Duration of incident-copilot agent runs.");

    public static readonly Counter<long> ToolCalls = Meter.CreateCounter<long>(
        "incidentcopilot.tool.calls",
        unit: "{call}",
        description: "Number of completed agent tool calls.");

    public static readonly Histogram<double> ToolDuration =
        Meter.CreateHistogram<double>(
            "incidentcopilot.tool.duration",
            unit: "s",
            description: "Duration of agent tool calls.");
}
