using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Application.Chat
{
    public record ChatSource(
    long ChunkId,
    string FileName,
    string? Section);
}
