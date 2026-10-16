using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Application.AI.Tools
{
    // pendingActionId is the idempotency key for state changing tools
    public sealed record ToolExecutionContext(string ArgumentsJson,Guid?PendingActionId=null);
    
    
}
