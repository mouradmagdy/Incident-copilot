using System.ComponentModel.DataAnnotations;

namespace IncidentCopilot.Api.Contracts.Chat;

public sealed record ChatRequest(
    [param: Required] string Message,
    Guid? ConversationId = null);
