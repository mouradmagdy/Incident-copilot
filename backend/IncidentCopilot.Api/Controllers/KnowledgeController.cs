using IncidentCopilot.Application.Knowledge;
using Microsoft.AspNetCore.Mvc;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/knowledge")]
public sealed class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeIndexer _indexer;

    public KnowledgeController(IKnowledgeIndexer indexer)
    {
        _indexer = indexer;
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(
        CancellationToken cancellationToken)
    {
        await _indexer.IndexAllAsync(cancellationToken);

        return NoContent();
    }
}
