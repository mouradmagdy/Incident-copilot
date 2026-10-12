using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Application.Knowledge;

public sealed record RetrievedChunk(
    int ChunkId,
    int DocumentId,
    string FileName,
    string? Section,
    string Content,
    double Distance);