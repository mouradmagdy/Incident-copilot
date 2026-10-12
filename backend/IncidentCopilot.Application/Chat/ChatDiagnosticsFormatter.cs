namespace IncidentCopilot.Application.Chat;

internal static class ChatDiagnosticsFormatter
{
    public static string CreateContentPreview(
        string content,
        int maximumLength)
    {
        var normalizedContent = content
            .ReplaceLineEndings(" ")
            .Trim();

        return normalizedContent.Length <= maximumLength
            ? normalizedContent
            : $"{normalizedContent[..maximumLength]}…";
    }
}
