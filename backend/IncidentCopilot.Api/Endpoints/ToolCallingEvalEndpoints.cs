using System.Net;
using IncidentCopilot.Api.Contracts.Evals;
using IncidentCopilot.Application.Chat;
using IncidentCopilot.Infrastructure.AI;

namespace IncidentCopilot.Api.Endpoints;

public static class ToolCallingEvalEndpoints
{
    public static IEndpointRouteBuilder MapToolCallingEvalEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost(
                "/api/evals/tool-calling",
                RunToolCallingEvalAsync)
            .WithTags("Evals")
            .Produces<ToolCallingEvalResponse>()
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> RunToolCallingEvalAsync(
        ToolCallingEvalRequest request,
        IChatService chatService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Message)] =
                    [
                        "Message cannot be empty."
                    ]
                });
        }

        try
        {
            var result = await chatService.RunAgentAsync(
                request.Message,
                cancellationToken);

            return Results.Ok(new ToolCallingEvalResponse(
                result.Content,
                result.ToolCalls,
                result.PendingAction is null
                    ? null
                    : new PendingActionEvalResponse(
                        result.PendingAction.ToolName,
                        result.PendingAction.Status)));
        }
        catch (GroqApiException exception)
            when (exception.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return Results.Json(
                new
                {
                    error = "Groq rate limit exceeded.",
                    detail = exception.ResponseBody,
                    retryAfter = exception.RetryAfter,
                    requestId = exception.RequestId,
                    rateLimitHeaders = exception.ResponseHeaders
                },
                statusCode: StatusCodes.Status429TooManyRequests);
        }
    }
}
