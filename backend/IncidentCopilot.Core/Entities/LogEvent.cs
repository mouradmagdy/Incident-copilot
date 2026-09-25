using IncidentCopilot.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Core.Entities
{
    public class LogEvent
    {
        public long Id { get; set; }

        public int ServiceId { get; set; }

        public MonitoredService Service { get; set; } = null!;

        public DateTime TimestampUtc { get; set; }

        public LogLevel Level { get; set; }

        public required string Message { get; set; }

        public string? ExceptionType { get; set; }

        public string? TraceId { get; set; }
    }
}
