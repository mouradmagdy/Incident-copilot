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

    public static readonly Counter<long> LlmCalls = Meter.CreateCounter<long>(
        "incidentcopilot.llm.calls",
        unit: "{call}",
        description: "Number of completed LLM calls.");

    public static readonly Histogram<double> LlmDuration =
        Meter.CreateHistogram<double>(
            "incidentcopilot.llm.duration",
            unit: "s",
            description: "Duration of LLM calls, including streamed responses.");

    public static readonly Counter<long> RetrievalCalls =
        Meter.CreateCounter<long>(
            "incidentcopilot.retrieval.calls",
            unit: "{call}",
            description: "Number of completed knowledge retrieval calls.");

    public static readonly Histogram<double> RetrievalDuration =
        Meter.CreateHistogram<double>(
            "incidentcopilot.retrieval.duration",
            unit: "s",
            description: "Duration of knowledge retrieval calls.");

    public static readonly Counter<long> ToolCalls = Meter.CreateCounter<long>(
        "incidentcopilot.tool.calls",
        unit: "{call}",
        description: "Number of completed agent tool calls.");

    public static readonly Histogram<double> ToolDuration =
        Meter.CreateHistogram<double>(
            "incidentcopilot.tool.duration",
            unit: "s",
            description: "Duration of agent tool calls.");

    public static readonly Counter<long> PendingActionsCreated =
        Meter.CreateCounter<long>(
            "incidentcopilot.pending_actions.created",
            unit: "{action}",
            description: "Number of pending actions created.");

    public static readonly Counter<long> PendingActionsConfirmed =
        Meter.CreateCounter<long>(
            "incidentcopilot.pending_actions.confirmed",
            unit: "{action}",
            description: "Number of pending actions confirmed.");

    public static readonly Counter<long> PendingActionsCancelled =
        Meter.CreateCounter<long>(
            "incidentcopilot.pending_actions.cancelled",
            unit: "{action}",
            description: "Number of pending actions cancelled.");

    public static readonly Counter<long> PendingActionsExpired =
        Meter.CreateCounter<long>(
            "incidentcopilot.pending_actions.expired",
            unit: "{action}",
            description: "Number of pending actions expired before confirmation.");

    public static readonly Counter<long> WriteToolFailures =
        Meter.CreateCounter<long>(
            "incidentcopilot.write_tools.failures",
            unit: "{failure}",
            description: "Number of failed state-changing tool executions.");
}
