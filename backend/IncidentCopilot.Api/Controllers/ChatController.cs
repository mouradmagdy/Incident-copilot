using IncidentCopilot.Api.Contracts.Chat;
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

        await foreach (var content in response.Content
            .WithCancellation(cancellationToken))
        {
            await WriteEventAsync(
                new { type = "delta", content },
                cancellationToken);
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
