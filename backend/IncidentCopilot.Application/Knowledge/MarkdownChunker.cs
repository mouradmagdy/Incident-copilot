namespace IncidentCopilot.Application.Knowledge;

public sealed class MarkdownChunker : IMarkdownChunker
{
    public IReadOnlyList<TextChunk> Chunk(
        string markdown,
        int targetChunkSize = 800,
        int chunkOverlap = 100)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetChunkSize);
        ArgumentOutOfRangeException.ThrowIfNegative(chunkOverlap);

        if (chunkOverlap >= targetChunkSize)
        {
            throw new ArgumentException(
                "Chunk overlap must be smaller than the target chunk size.",
                nameof(chunkOverlap));
        }

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }
        var blocks = markdown
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<TextChunk>();
        var currentParts= new List<string>();
        var currentLength = 0;
        string? currentSection = null;
        foreach(var block in blocks)
        {
            if (block.StartsWith("#"))
            {
                currentSection = block.TrimStart('#').Trim();
            }
            var additionalLength = block.Length + (currentParts.Count > 0 ? 2 : 0);

            if (currentParts.Count > 0 && currentLength + additionalLength > targetChunkSize)
            {
                AddChunk(chunks, currentParts, currentSection);

                currentParts = GetOverlapParts(currentParts, chunkOverlap);
                currentLength = CalculateLength(currentParts);
            }
            currentParts.Add(block);
            currentLength += additionalLength;
        }
        if (currentParts.Count > 0)
        {
            AddChunk(chunks, currentParts, currentSection);
        }
        return chunks;
    }
    private static void AddChunk(
          List<TextChunk> chunks,
          List<string> parts,
          string? section)
    {
        var content = string.Join("\n\n", parts).Trim();

        if (string.IsNullOrWhiteSpace(content))
            return;

        chunks.Add(new TextChunk(
            chunks.Count,
            content,
            section));
    }

    private static List<string> GetOverlapParts(
        List<string> parts,
        int chunkOverlap)
    {
        if (chunkOverlap == 0)
        {
            return [];
        }

        var overlap = new List<string>();
        var length = 0;

        for (var i = parts.Count - 1; i >= 0; i--)
        {
            var part = parts[i];

            if (length + part.Length > chunkOverlap &&
                overlap.Count > 0)
            {
                break;
            }

            overlap.Insert(0, part);
            length += part.Length;

            if (length >= chunkOverlap)
                break;
        }

        return overlap;
    }

    private static int CalculateLength(
        List<string> parts)
    {
        return string.Join("\n\n", parts).Length;
    }

}
