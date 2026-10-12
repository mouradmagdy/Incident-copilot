using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentCopilot.Application.Chat
{
    public class ChatService : IChatService
    {
        private readonly IKnowledgeRetriever _retriever;
        private readonly ILlmClient _llmClient;
        private const int TopK = 4;
        private const double MaxDistance = 0.75;

        public ChatService(IKnowledgeRetriever retriever, ILlmClient llmClient)
        {
            _retriever = retriever;
            _llmClient = llmClient;
        }

        public async Task<ChatResponse>AskAsync(string message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(
                    "Message cannot be empty.",
                    nameof(message));
            }
            var retrievedChunks = await _retriever.SearchAsync(message, TopK, cancellationToken);

            var relevantChunks = retrievedChunks.Where(c => c.Distance <= MaxDistance).ToList();
            if (relevantChunks.Count == 0)
            {
                return new ChatResponse(
                    "I don't have relevant internal documentation to answer that question.",
                    []);
            }

            var context = BuildContext(relevantChunks);
            var systemPrompt = BuildSystemPrompt(context);

            var llmRequest = new LlmRequest(systemPrompt, message);
            var llmResponse = await _llmClient.GenerateAsync(llmRequest, cancellationToken);

            // dont ask the llm to return the sources it can hallucinate them, instead we will return the sources we used to build the context
            var sources = relevantChunks.Select(s => new ChatSource(s.ChunkId, s.FileName, s.Section)).Distinct().ToList();

            return new ChatResponse(llmResponse.Content, sources);
        }

        // this is the augmentation part of RAG
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
}
