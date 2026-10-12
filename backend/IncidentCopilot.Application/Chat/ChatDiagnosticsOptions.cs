namespace IncidentCopilot.Application.Chat;

public sealed class ChatDiagnosticsOptions
{
    public const string SectionName = "ChatDiagnostics";

    public bool IncludeChunkContent { get; set; }

    public int ChunkContentLimit { get; set; } = 800;
}
