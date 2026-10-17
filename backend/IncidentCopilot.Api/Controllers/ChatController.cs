using IncidentCopilot.Api.Contracts.Chat;
using IncidentCopilot.Api.Contracts.PendingActions;
using IncidentCopilot.Application.Chat;
using IncidentCopilot.Application.Conversations;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
  
    private readonly IChatService _chatService;
    private readonly IConversationStore _conversations;

    public ChatController(
        IChatService chatService,
        IConversationStore conversations)
    {
        _chatService = chatService;
        _conversations = conversations;
    }

    [HttpGet("conversations")]
    [ProducesResponseType<IReadOnlyList<ConversationSummary>>(
        StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ConversationSummary>> ListConversations(
        CancellationToken cancellationToken)
    {
        return _conversations.ListAsync(cancellationToken);
    }

    [HttpGet("conversations/{id:guid}")]
    [ProducesResponseType<ConversationView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationView>> GetConversation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetAsync(
            id,
            cancellationToken);

        return conversation is null
            ? NotFound()
            : Ok(conversation);
    }

    [HttpPost]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task Chat(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        var conversationId = request.ConversationId;

        if (conversationId is null)
        {
            var conversation = await _conversations.CreateAsync(
                request.Message,
                cancellationToken);
            conversationId = conversation.Id;
        }

        var userMessageAdded = await _conversations.AddMessageAsync(
            conversationId.Value,
            new AddConversationMessageRequest(
                "user",
                request.Message),
            cancellationToken);

        if (!userMessageAdded)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var response = await _chatService.AskStreamAsync(
            request.Message,
            cancellationToken);

        var content = new StringBuilder();
        Guid? pendingActionId = null;
        ChatDebugInfo? debug = null;

        Response.ContentType = "application/x-ndjson";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await WriteEventAsync(
            new
            {
                type = "conversation",
                conversationId
            },
            cancellationToken);

        await foreach (var chatEvent in response.Events
            .WithCancellation(cancellationToken))
        {
            switch (chatEvent)
            {
                case ChatDeltaEvent delta:
                    content.Append(delta.Content);
                    await WriteEventAsync(
                        new
                        {
                            type = "delta",
                            content = delta.Content
                        },
                        cancellationToken);
                    break;

                case ChatPendingActionEvent pending:
                    pendingActionId = pending.Action.Id;
                    await WriteEventAsync(
                        new
                        {
                            type = "pending_action",
                            action = PendingActionResponse.From(
                                pending.Action)
                        },
                        cancellationToken);
                    break;

                case ChatDebugEvent debugEvent:
                    debug = debugEvent.Debug;
                    await WriteEventAsync(
                        new
                        {
                            type = "debug",
                            debug
                        },
                        cancellationToken);
                    break;
            }
        }

        await WriteEventAsync(
            new { type = "sources", sources = response.Sources },
            cancellationToken);

        await _conversations.AddMessageAsync(
            conversationId.Value,
            new AddConversationMessageRequest(
                "assistant",
                content.ToString(),
                response.Sources,
                debug,
                pendingActionId),
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
