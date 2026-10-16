using System.Text.Json;
using IncidentCopilot.Core.AI;

namespace IncidentCopilot.Application.AI.Tools;

public sealed class CreateIncidentApprovalPolicy : IToolApprovalPolicy
{
    private const double AverageLatencyThresholdMs = 1_000;
    private const double P95LatencyThresholdMs = 2_000;
    private const double ErrorRateThresholdPercent = 5;
    private const int QueueDepthThreshold = 50;
    private const int ActiveDbConnectionsThreshold = 90;
    private const int RequiredBreachCount = 2;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ToolName => "create_incident";

    public ToolApprovalDecision Evaluate(
        string argumentsJson,
        IReadOnlyList<LlmToolExchange> toolExchanges)
    {
        CreateIncidentPolicyArguments? arguments;

        try
        {
            arguments = JsonSerializer
                .Deserialize<CreateIncidentPolicyArguments>(
                    argumentsJson,
                    SerializerOptions);
        }
        catch (JsonException)
        {
            return Deny("The incident proposal arguments were invalid.");
        }

        if (string.IsNullOrWhiteSpace(arguments?.ServiceName))
        {
            return Deny(
                "The incident proposal did not identify a monitored service.");
        }

        var metricsExchange = toolExchanges
            .LastOrDefault(exchange =>
                exchange.ToolCall.Name == "get_service_metrics");

        if (metricsExchange is null)
        {
            return Deny(
                "No current service metrics were collected before the incident proposal.");
        }

        ServiceMetricsEvidence? metrics;

        try
        {
            metrics = JsonSerializer.Deserialize<ServiceMetricsEvidence>(
                metricsExchange.ToolResult,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return Deny(
                "The current service metrics could not be evaluated.");
        }

        if (metrics is null ||
            !string.Equals(
                metrics.ServiceName,
                arguments.ServiceName,
                StringComparison.OrdinalIgnoreCase))
        {
            return Deny(
                "No current metrics were collected for the service in the incident proposal.");
        }

        var breaches = new List<string>();

        if (metrics.AverageLatencyMs >= AverageLatencyThresholdMs)
        {
            breaches.Add(
                $"average latency {metrics.AverageLatencyMs:F0} ms");
        }

        if (metrics.P95LatencyMs >= P95LatencyThresholdMs)
        {
            breaches.Add(
                $"p95 latency {metrics.P95LatencyMs:F0} ms");
        }

        if (metrics.ErrorRatePercent >= ErrorRateThresholdPercent)
        {
            breaches.Add(
                $"error rate {metrics.ErrorRatePercent:F2}%");
        }

        if (metrics.QueueDepth >= QueueDepthThreshold)
        {
            breaches.Add($"queue depth {metrics.QueueDepth}");
        }

        if (metrics.ActiveDbConnections >=
            ActiveDbConnectionsThreshold)
        {
            breaches.Add(
                $"active DB connections {metrics.ActiveDbConnections}");
        }

        if (breaches.Count < RequiredBreachCount)
        {
            return Deny(
                "Current metrics are within the healthy range and do not " +
                "meet the serious-production-issue threshold.");
        }

        return new ToolApprovalDecision(
            true,
            "Serious production thresholds were exceeded: " +
            string.Join(", ", breaches) + ".");
    }

    private static ToolApprovalDecision Deny(string reason)
    {
        return new ToolApprovalDecision(false, reason);
    }

    private sealed record CreateIncidentPolicyArguments(
        string? ServiceName);

    private sealed record ServiceMetricsEvidence(
        string? ServiceName,
        double AverageLatencyMs,
        double P95LatencyMs,
        double ErrorRatePercent,
        int QueueDepth,
        int ActiveDbConnections);
}
