using IncidentCopilot.Core.AI;
using IncidentCopilot.Core.Entities;
using Microsoft.Extensions.Options;
using Pgvector;

namespace IncidentCopilot.Application.Knowledge;

public sealed class KnowledgeIndexer : IKnowledgeIndexer
{
    private readonly IMarkdownChunker _chunker;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IKnowledgeDocumentRepository _repository;
    private readonly KnowledgeOptions _options;

    public KnowledgeIndexer(
        IEmbeddingClient embeddingClient,
        IMarkdownChunker chunker,
        IKnowledgeDocumentRepository repository,
        IOptions<KnowledgeOptions> options)
    {
        _embeddingClient = embeddingClient;
        _chunker = chunker;
        _repository = repository;
        _options = options.Value;
    }

    public async Task IndexAllAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_options.RunbooksPath))
        {
            throw new DirectoryNotFoundException(
                $"Runbook directory not found: {_options.RunbooksPath}");
        }

        var files = Directory.GetFiles(
            _options.RunbooksPath,
            "*.md",
            SearchOption.TopDirectoryOnly);

        var documents = new List<KnowledgeDocument>();

        foreach (var file in files)
        {
            var markdown = await File.ReadAllTextAsync(file, cancellationToken);
            var chunks = _chunker.Chunk(markdown);

            if (chunks.Count == 0)
            {
                continue;
            }

            var texts = chunks.Select(chunk => chunk.content).ToArray();

            var embeddings = await _embeddingClient.EmbedAsync(
                texts,
                cancellationToken);

            if (embeddings.Count != chunks.Count)
            {
                throw new InvalidOperationException(
                    "Embeddings count does not match chunks count.");
            }

            var document = new KnowledgeDocument
            {
                FileName = Path.GetFileName(file),
                Title = GetTitle(markdown)
                    ?? Path.GetFileNameWithoutExtension(file),
                IndexedAt = DateTime.UtcNow
            };

            for (var index = 0; index < chunks.Count; index++)
            {
                document.Chunks.Add(new KnowledgeDocumentChunk
                {
                    ChunkIndex = chunks[index].index,
                    Content = chunks[index].content,
                    Section = chunks[index].section,
                    Embedding = new Vector(embeddings[index])
                });
            }

            documents.Add(document);
        }

        await _repository.ReplaceAllAsync(documents, cancellationToken);
    }

    private static string? GetTitle(string markdown)
    {
        return markdown
            .Split('\n')
            .Select(x => x.Trim())
            .FirstOrDefault(x => x.StartsWith("# "))
            ?.TrimStart('#')
            .Trim();
    }
}
