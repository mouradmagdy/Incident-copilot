using System.Diagnostics;
using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Core.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IncidentCopilot.Application.Chat;

public sealed class ChatService : IChatService
{
    private const int TopK = 4;
    private const double MaxDistance = 0.75;

    private readonly IKnowledgeRetriever _retriever;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<ChatService> _logger;
    private readonly ChatDiagnosticsOptions _diagnosticsOptions;

    public ChatService(
        IKnowledgeRetriever retriever,
        ILlmClient llmClient,
        ILogger<ChatService> logger,
        IOptions<ChatDiagnosticsOptions> diagnosticsOptions)
    {
        _retriever = retriever;
        _llmClient = llmClient;
        _logger = logger;
        _diagnosticsOptions = diagnosticsOptions.Value;
    }

    public async Task<ChatResponse> AskAsync(
        string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(message));
        }

        _logger.LogInformation(
            "Chat query:{NewLine}{Query}",
            Environment.NewLine,
            message);

        var retrievedChunks = await _retriever.SearchAsync(
            message,
            TopK,
            cancellationToken);

        var relevantChunks = retrievedChunks
            .Where(chunk => chunk.Distance <= MaxDistance)
            .ToList();

        _logger.LogInformation(
            "Retrieved chunks: {RetrievedChunkCount}. Number surviving threshold: {SurvivingChunkCount}. Maximum distance: {MaxDistance:F2}.",
            retrievedChunks.Count,
            relevantChunks.Count,
            MaxDistance);

        LogRetrievedChunks(retrievedChunks);

        if (relevantChunks.Count == 0)
        {
            _logger.LogInformation(
                "LLM duration: {LlmDurationMs} ms. LLM call skipped because no chunks survived the distance threshold.",
                0);

            return new ChatResponse(
                "I don't have relevant internal documentation to answer that question.",
                []);
        }

        var context = BuildContext(relevantChunks);
        var systemPrompt = BuildSystemPrompt(context);
        var llmRequest = new LlmRequest(systemPrompt, message);
        LlmResponse llmResponse;
        var llmStopwatch = Stopwatch.StartNew();

        try
        {
            llmResponse = await _llmClient.GenerateAsync(
                llmRequest,
                cancellationToken);
        }
        finally
        {
            llmStopwatch.Stop();
            _logger.LogInformation(
                "LLM duration: {LlmDurationMs:F0} ms.",
                llmStopwatch.Elapsed.TotalMilliseconds);
        }

        // Sources come from retrieved context, never from the LLM response.
        var sources = relevantChunks
            .DistinctBy(chunk => (chunk.FileName, chunk.Section))
            .Select(chunk => new ChatSource(
                chunk.ChunkId,
                chunk.FileName,
                chunk.Section))
            .ToList();

        return new ChatResponse(llmResponse.Content, sources);
    }

    private void LogRetrievedChunks(
        IReadOnlyList<RetrievedChunk> chunks)
    {
        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            var survivesThreshold = chunk.Distance <= MaxDistance;

            _logger.LogInformation(
                """
                Chunk {ChunkRank}
                {FileName}
                Section: {Section}
                Distance: {Distance:F3}
                Survives threshold: {SurvivesThreshold}
                """,
                index + 1,
                chunk.FileName,
                chunk.Section ?? "Unknown",
                chunk.Distance,
                survivesThreshold);

            if (_diagnosticsOptions.IncludeChunkContent)
            {
                _logger.LogInformation(
                    """
                    Chunk {ChunkRank} content preview ({ChunkContentLength} characters total)
                    {ChunkContentPreview}
                    """,
                    index + 1,
                    chunk.Content.Length,
                    ChatDiagnosticsFormatter.CreateContentPreview(
                        chunk.Content,
                        _diagnosticsOptions.ChunkContentLimit));
            }
        }
    }

    // This is the augmentation part of RAG.
    private static string BuildContext(
        IReadOnlyList<RetrievedChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return "No relevant internal documentation was found.";
        }

        return string.Join(
            "\n\n---\n\n",
            chunks.Select(chunk =>
                $"""
                Source: {chunk.FileName}
                Section: {chunk.Section ?? "Unknown"}

                {chunk.Content}
                """));
    }

    private static string BuildSystemPrompt(string context)
    {
        return $"""
            You are an assistant helping software engineers investigate
            production incidents.

            You have access to internal operational documentation supplied
            below.

            Use the documentation when it is relevant to the user's question.

            Do not invent system state, metrics, logs, incidents, deployments,
            or root causes.

            Internal documentation describes procedures and expected system
            behavior. It does not represent current live system state.

            If the user asks about current runtime information and that
            information is not provided, clearly state that you do not have
            access to the required live data.

            If the internal documentation does not contain enough information
            to answer the question, say so.

            Internal documentation:

            {context}
            """;
    }
}
