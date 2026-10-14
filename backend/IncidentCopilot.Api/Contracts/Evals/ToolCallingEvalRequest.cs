using System.ComponentModel.DataAnnotations;

namespace IncidentCopilot.Api.Contracts.Evals;

public sealed record ToolCallingEvalRequest(
    [param: Required] string Message);
