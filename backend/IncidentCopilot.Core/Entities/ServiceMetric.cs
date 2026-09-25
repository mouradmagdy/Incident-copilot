using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Core.Entities
{
    public class ServiceMetric
    {
        public long Id { get; set; }

        public int ServiceId { get; set; }

        public MonitoredService Service { get; set; } = null!;

        public DateTime TimestampUtc { get; set; }

        public double RequestsPerSecond { get; set; }

        public double AverageLatencyMs { get; set; }

        public double P95LatencyMs { get; set; }

        public double ErrorRatePercent { get; set; }

        public double CpuUsagePercent { get; set; }

        public double MemoryUsagePercent { get; set; }

        public int QueueDepth { get; set; }

        public int ActiveDbConnections { get; set; }
    }
}
