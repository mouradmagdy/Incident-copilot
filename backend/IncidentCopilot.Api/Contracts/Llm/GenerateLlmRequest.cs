using System.ComponentModel.DataAnnotations;

namespace IncidentCopilot.Api.Contracts.Llm;

public sealed record GenerateLlmRequest(
    [param: Required] string SystemPrompt,
    [param: Required] string UserMessage);
