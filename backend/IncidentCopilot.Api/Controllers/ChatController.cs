using IncidentCopilot.Api.Contracts.Chat;
using IncidentCopilot.Api.Contracts.PendingActions;
using IncidentCopilot.Application.Chat;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task Chat(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _chatService.AskStreamAsync(
            request.Message,
            cancellationToken);

        Response.ContentType = "application/x-ndjson";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await foreach (var chatEvent in response.Events
            .WithCancellation(cancellationToken))
        {
            switch (chatEvent)
            {
                case ChatDeltaEvent delta:
                    await WriteEventAsync(
                        new
                        {
                            type = "delta",
                            content = delta.Content
                        },
                        cancellationToken);
                    break;

                case ChatPendingActionEvent pending:
                    await WriteEventAsync(
                        new
                        {
                            type = "pending_action",
                            action = PendingActionResponse.From(
                                pending.Action)
                        },
                        cancellationToken);
                    break;
            }
        }

        await WriteEventAsync(
            new { type = "sources", sources = response.Sources },
            cancellationToken);
    }

    private async Task WriteEventAsync<T>(
        T value,
        CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(
            Response.Body,
            value,
            JsonSerializerOptions.Web,
            cancellationToken);
        await Response.WriteAsync("\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
