using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Application.Knowledge
{
    public interface IKnowledgeRetriever
    {
        Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default);
    }
}
