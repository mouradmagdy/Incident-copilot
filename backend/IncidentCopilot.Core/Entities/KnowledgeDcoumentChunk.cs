using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Core.Entities
{
    public class KnowledgeDocumentChunk
    {
        public int Id { get; set; }

        public long DocumentId { get; set; }
        public KnowledgeDocument Document { get; set; } = null!;
        public int chunkIndex{get;set;}
        public required string Content{get;set;}
        public string? Section{get;set;}

    }
}
