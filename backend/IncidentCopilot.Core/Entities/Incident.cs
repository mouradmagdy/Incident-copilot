using IncidentCopilot.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Core.Entities
{
    public class Incident
    {
        public int Id { get; set; }

        public required string Title { get; set; }

        public string? Description { get; set; }

        public IncidentSeverity Severity { get; set; }

        public IncidentStatus Status { get; set; }

        public DateTime StartedAtUtc { get; set; }

        public DateTime? ResolvedAtUtc { get; set; }

        public string? RootCause { get; set; }
    }
}
