using IncidentCopilot.Api.Contracts.Chat;
using IncidentCopilot.Application.Chat;
using IncidentCopilot.Core.AI;
using Microsoft.AspNetCore.Mvc;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
  
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Chat(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _chatService.AskAsync(
            request.Message,
            cancellationToken);

        return Ok(response);
    }
}
