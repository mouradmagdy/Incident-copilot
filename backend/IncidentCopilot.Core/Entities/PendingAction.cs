using IncidentCopilot.Core.Enums;

namespace IncidentCopilot.Core.Entities
{
    public class PendingAction
    {
        public Guid Id { get; set; }
        public required string ToolName { get; set; }
        public required string ArgumentsJson { get; set; }
        public required string Description { get; set; }
        public required string ToolCallId { get; set; }
        public required string SystemPrompt { get; set; }
        public required string UserMessage { get; set; }
        public required string ToolExchangesJson { get; set; }
        public PendingActionStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public DateTime? DecidedAtUtc { get; set; }

        public string? ResultJson { get; set; }

        public string? Error { get; set; }

        public string? AssistantResponse { get; set; }

    }
}
