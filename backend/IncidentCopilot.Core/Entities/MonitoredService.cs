using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Core.Entities
{
    public class MonitoredService
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<ServiceMetric> Metrics { get; set; } = [];
        public ICollection<LogEvent> Logs { get; set; } = [];
    }
}
