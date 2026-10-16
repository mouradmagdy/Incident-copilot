namespace IncidentCopilot.Application.AI.Tools;

public enum ToolFault
{
    None,
    MetricsTimeout,
    LogsTimeout,
    MetricsFailure,
    LogsFailure,
    RagPromptInjection,
    LogsPromptInjection,
    MetricsPromptInjection,
    HealthyMetrics,
    SeriousMetrics
}
