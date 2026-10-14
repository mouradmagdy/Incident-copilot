namespace IncidentCopilot.Infrastructure.AI.Tools.GetRecentLogs;

internal sealed class GetRecentLogsArguments
{
    public string ServiceName { get; init; } = string.Empty;

    public int Minutes { get; init; } = 15;

    public int MaxResults { get; init; } = 20;
}
