using IncidentCopilot.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Core.Entities
{
    public class PendingAction
    {
        public Guid Id { get; set; }
        public required string ToolName { get; set; }
        public required string ArgumentsJson { get; set; }
        public required string Description { get; set; }
        public PendingActionStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public DateTime? DecidedAtUtc { get; set; }

        public string? ResultJson { get; set; }

        public string? Error { get; set; }

    }
}
