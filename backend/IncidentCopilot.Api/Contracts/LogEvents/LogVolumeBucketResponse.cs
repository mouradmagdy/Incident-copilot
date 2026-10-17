namespace IncidentCopilot.Api.Contracts.LogEvents;

public sealed record LogVolumeBucketResponse(
    DateTime BucketStartUtc,
    int InfoCount,
    int WarningCount,
    int ErrorCount);
