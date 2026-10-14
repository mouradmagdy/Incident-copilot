using IncidentCopilot.Application.Chat;

namespace IncidentCopilot.Api.Contracts.Evals;

public sealed record ToolCallingEvalResponse(
    string Answer,
    IReadOnlyList<ToolCallTrace> ToolCalls);
