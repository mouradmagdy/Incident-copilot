namespace IncidentCopilot.Application.Chat;

public sealed record ChatToolCallDebug(
    string Name,
    double? DurationMs,
    bool? Success);

public sealed record ChatDebugInfo(
    IReadOnlyList<ChatSource> Sources,
    IReadOnlyList<ChatToolCallDebug> ToolCalls,
    int LlmRoundCount,
    double TotalLatencyMs,
    string PendingActionState);
