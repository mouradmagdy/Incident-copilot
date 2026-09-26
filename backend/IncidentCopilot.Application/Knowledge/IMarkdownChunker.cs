namespace IncidentCopilot.Application.Knowledge;


public record TextChunk(int index,string content,string?section);

public interface IMarkdownChunker
{
    IReadOnlyList<TextChunk> Chunk(string markdown);
}
