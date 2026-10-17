using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Application.Chat
{
    public interface IChatService
    {
        Task<ChatStreamResponse> AskStreamAsync(
            string message,
            CancellationToken cancellationToken = default);

        Task<ChatStreamResponse> AskStreamAsync(
            string message,
            Guid? userId,
            CancellationToken cancellationToken = default);

        Task<AgentRunResult> RunAgentAsync(
            string message,
            CancellationToken cancellationToken = default);
    }
}
