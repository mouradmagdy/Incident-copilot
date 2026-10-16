using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Infrastructure.AI.Tools.CreateIncident
{
    public sealed record CreateIncidentArguments(
        string ServiceName,
        string Title,
        string? Description,
        string Severity);
}
