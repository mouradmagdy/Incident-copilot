using IncidentCopilot.Api.Contracts.Chat;
using IncidentCopilot.Core.AI;
using Microsoft.AspNetCore.Mvc;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private const string SystemPrompt =
        "You are Incident Copilot, an assistant helping software engineers " +
        "investigate production incidents. Answer clearly. If you do not " +
        "have enough information to determine the cause, say so.";

    private readonly ILlmClient _llmClient;

    public ChatController(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    [HttpPost]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Chat(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _llmClient.GenerateAsync(
            new LlmRequest(SystemPrompt, request.Message),
            cancellationToken);

        return Ok(new ChatResponse(response.Content));
    }
}
