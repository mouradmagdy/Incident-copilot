using IncidentCopilot.Api.Contracts.Llm;
using IncidentCopilot.Core.AI;
using Microsoft.AspNetCore.Mvc;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/llm")]
public sealed class LlmController : ControllerBase
{
    private readonly ILlmClient _llmClient;

    public LlmController(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    [HttpPost("generate")]
    [ProducesResponseType<GenerateLlmResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenerateLlmResponse>> Generate(
        GenerateLlmRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _llmClient.GenerateAsync(
            new LlmRequest(
                request.SystemPrompt,
                request.UserMessage),
            cancellationToken);

        return Ok(new GenerateLlmResponse(response.Content));
    }
}
