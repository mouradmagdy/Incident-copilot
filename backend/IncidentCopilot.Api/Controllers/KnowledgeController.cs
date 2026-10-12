using IncidentCopilot.Application.Knowledge;
using Microsoft.AspNetCore.Mvc;

namespace IncidentCopilot.Api.Controllers;

[ApiController]
[Route("api/knowledge")]
public sealed class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeIndexer _indexer;
    private readonly IKnowledgeRetriever _retriever;

    public KnowledgeController(
        IKnowledgeIndexer indexer,
        IKnowledgeRetriever retriever)
    {
        _indexer = indexer;
        _retriever = retriever;
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(
        [FromQuery] int? targetChunkSize = null,
        [FromQuery] int? chunkOverlap = null,
        CancellationToken cancellationToken = default)
    {
        if ((targetChunkSize.HasValue || chunkOverlap.HasValue)
            && (!targetChunkSize.HasValue
                || !chunkOverlap.HasValue
                || targetChunkSize <= 0
                || chunkOverlap < 0
                || chunkOverlap >= targetChunkSize))
        {
            return BadRequest(
                "Provide a positive targetChunkSize and a chunkOverlap " +
                "between zero and targetChunkSize - 1.");
        }

        await _indexer.IndexAllAsync(
            targetChunkSize,
            chunkOverlap,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("search")]
    [ProducesResponseType<IReadOnlyList<RetrievedChunk>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RetrievedChunk>>> Search(
        [FromQuery] string query,
        [FromQuery] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            ModelState.AddModelError(
                nameof(query),
                "A non-empty search query is required.");
        }

        if (topK is < 1 or > 50)
        {
            ModelState.AddModelError(
                nameof(topK),
                "topK must be between 1 and 50.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return Ok(await _retriever.SearchAsync(
            query,
            topK,
            cancellationToken));
    }
}
