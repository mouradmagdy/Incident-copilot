namespace IncidentCopilot.Infrastructure.AI.Tools.GetServiceMetrics;

internal sealed record GetServiceMetricsArguments(
    string ServiceName,
    int Minutes = 15);
