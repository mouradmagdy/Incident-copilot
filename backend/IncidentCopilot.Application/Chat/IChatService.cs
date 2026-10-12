using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Application.Chat
{
    public interface IChatService
    {
        Task<ChatResponse> AskAsync(string message, CancellationToken cancellationToken = default);
    }
}
