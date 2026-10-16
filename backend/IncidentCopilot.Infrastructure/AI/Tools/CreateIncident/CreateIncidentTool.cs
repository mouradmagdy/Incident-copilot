using System;
using System.Text.Json;
using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Core.Entities;
using IncidentCopilot.Core.Enums;
using IncidentCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentCopilot.Infrastructure.AI.Tools.CreateIncident
{
    public sealed class CreateIncidentTool : IAgentTool
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IncidentCopilotDbContext _dbContext;

        public CreateIncidentTool(IncidentCopilotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string Name => "create_incident";

        public ToolExecutionMode ExecutionMode =>
            ToolExecutionMode.RequiresApproval;

        public ToolDefinition Definition => new(
            Name,
            "Proposes creating a production incident and displays Confirm and " +
            "Cancel controls to the user; it does not create the incident until " +
            "the user confirms. Request this after current runtime evidence " +
            "shows a serious production issue. Do not ask for confirmation in " +
            "ordinary assistant text; call this tool to request confirmation.",
            new
            {
                type = "object",
                properties = new
                {
                    serviceName = new
                    {
                        type = "string",
                        description =
                            "Exact monitored service name, for example OrderService."
                    },
                    title = new
                    {
                        type = "string",
                        description = "A concise incident title."
                    },
                    description = new
                    {
                        type = "string",
                        description = "The runtime evidence and observed symptoms."
                    },
                    severity = new
                    {
                        type = "string",
                        @enum = new[] { "Low", "Medium", "High", "Critical" }
                    }
                },
                required = new[]
                {
                    "serviceName",
                    "title",
                    "severity"
                },
                additionalProperties = false
            });

        public string GetApprovalDescription(string argumentsJson)
        {
            try
            {
                var arguments = JsonSerializer
                    .Deserialize<CreateIncidentArguments>(
                        argumentsJson,
                        SerializerOptions);

                return arguments is null
                    ? "Create a new incident."
                    : $"Create {arguments.Severity} incident: {arguments.Title}";
            }
            catch (JsonException)
            {
                return "Create a new incident.";
            }
        }

        public async Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (context.PendingActionId is not Guid pendingActionId)
            {
                return new ToolResult(
                    false,
                    string.Empty,
                    "Creating an incident requires an approved pending action.");
            }

            var existingIncident = await _dbContext.Incidents
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    incident =>
                        incident.PendingActionId == pendingActionId,
                    cancellationToken);

            if (existingIncident is not null)
            {
                return CreateSuccessResult(existingIncident);
            }

            CreateIncidentArguments? arguments;

            try
            {
                arguments = JsonSerializer
                    .Deserialize<CreateIncidentArguments>(
                        context.ArgumentsJson,
                        SerializerOptions);
            }
            catch (JsonException)
            {
                return new ToolResult(
                    false,
                    string.Empty,
                    "Tool arguments were not valid JSON.");
            }

            if (arguments is null ||
                string.IsNullOrWhiteSpace(arguments.ServiceName) ||
                string.IsNullOrWhiteSpace(arguments.Title))
            {
                return new ToolResult(
                    false,
                    string.Empty,
                    "serviceName and title are required.");
            }

            if (!Enum.TryParse<IncidentSeverity>(
                    arguments.Severity,
                    ignoreCase: true,
                    out var severity) ||
                !Enum.IsDefined(severity))
            {
                return new ToolResult(
                    false,
                    string.Empty,
                    $"Unknown severity '{arguments.Severity}'.");
            }

            var incident = new Incident
            {
                Title = arguments.Title.Trim(),
                Description = arguments.Description,
                Severity = severity,
                Status = IncidentStatus.Open,
                StartedAtUtc = DateTime.UtcNow,
                PendingActionId = pendingActionId
            };

            _dbContext.Incidents.Add(incident);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return CreateSuccessResult(incident);
        }

        private static ToolResult CreateSuccessResult(Incident incident)
        {
            return new ToolResult(
                true,
                JsonSerializer.Serialize(new
                {
                    incident.Id,
                    incident.Title,
                    Severity = incident.Severity.ToString(),
                    Status = incident.Status.ToString(),
                    incident.StartedAtUtc
                }));
        }

    }
}
