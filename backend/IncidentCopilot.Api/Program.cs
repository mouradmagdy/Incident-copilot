using IncidentCopilot.Api.Endpoints;
using IncidentCopilot.Api.Controllers;
using IncidentCopilot.Api.Security;
using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.Chat;
using IncidentCopilot.Application.Conversations;
using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Application.Observability;
using IncidentCopilot.Application.PendingActions;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.AI;
using IncidentCopilot.Infrastructure.AI.Tools.GetRecentLogs;
using IncidentCopilot.Infrastructure.AI.Tools.GetServiceMetrics;
using IncidentCopilot.Infrastructure.AI.Tools.CreateIncident;
using IncidentCopilot.Infrastructure.Persistence;
using IncidentCopilot.Infrastructure.Simulation;
using IncidentCopilot.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Pgvector.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options => options.SigningKey.Length >= 32,
        "Jwt:SigningKey must contain at least 32 characters.")
    .Validate(
        options => options.AccessTokenMinutes is > 0 and <= 10_080,
        "Jwt:AccessTokenMinutes must be between 1 and 10080.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[
                    AuthController.AccessTokenCookieName];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var developmentOtlpEndpoint = builder.Environment.IsDevelopment()
    ? new Uri(
        builder.Configuration["OpenTelemetry:Otlp:Endpoint"]
            ?? "http://localhost:4317",
        UriKind.Absolute)
    : null;

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "IncidentCopilot.Api",
        serviceVersion: typeof(Program).Assembly
            .GetName()
            .Version?
            .ToString()))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(IncidentCopilotTelemetry.Name)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (developmentOtlpEndpoint is not null)
        {
            tracing.AddOtlpExporter(options =>
                options.Endpoint = developmentOtlpEndpoint);
        }
        else
        {
            tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(IncidentCopilotTelemetry.Name)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (developmentOtlpEndpoint is not null)
        {
            metrics.AddOtlpExporter((options, readerOptions) =>
            {
                options.Endpoint = developmentOtlpEndpoint;
                readerOptions.PeriodicExportingMetricReaderOptions
                    .ExportIntervalMilliseconds = 10_000;
            });
        }
        else
        {
            metrics.AddConsoleExporter((_, readerOptions) =>
            {
                readerOptions.PeriodicExportingMetricReaderOptions
                    .ExportIntervalMilliseconds = 10_000;
            });
        }
    });

builder.Services
    .AddOptions<GroqOptions>()
    .Bind(builder.Configuration.GetSection(GroqOptions.SectionName))
    .PostConfigure(options =>
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            options.ApiKey = builder.Configuration["GROQ_API_KEY"]
                ?? string.Empty;
        }
    })
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ApiKey),
        "A Groq API key must be configured using Groq:ApiKey, " +
        "Groq__ApiKey, or GROQ_API_KEY.")
    .Validate(
        options => Uri.TryCreate(
            options.BaseUrl,
            UriKind.Absolute,
            out _),
        "Groq:BaseUrl must be an absolute URL.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Model),
        "Groq:Model must be configured.");

builder.Services.AddHttpClient<GroqLlmClient>();
builder.Services.AddScoped<ILlmClient>(serviceProvider =>
    new InstrumentedLlmClient(
        serviceProvider.GetRequiredService<GroqLlmClient>()));
builder.Services.AddScoped<IAgentTool, GetServiceMetricsTool>();
builder.Services.AddScoped<IAgentTool, GetRecentLogsTool>();
builder.Services.AddScoped<IAgentTool, CreateIncidentTool>();
builder.Services.AddScoped<ToolExecutor>();
builder.Services.AddSingleton<PendingActionExecutionGate>();
builder.Services.AddSingleton<
    IToolApprovalPolicy,
    CreateIncidentApprovalPolicy>();
builder.Services.AddSingleton<IMarkdownChunker, MarkdownChunker>();
builder.Services.AddScoped<IKnowledgeIndexer, KnowledgeIndexer>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IConversationStore, ConversationStore>();
builder.Services.AddScoped<
    IPendingActionService,
    PendingActionService>();

builder.Services
    .AddOptions<KnowledgeOptions>()
    .Bind(builder.Configuration.GetSection(KnowledgeOptions.SectionName))
    .PostConfigure(options =>
    {
        if (!Path.IsPathRooted(options.RunbooksPath))
        {
            options.RunbooksPath = Path.GetFullPath(
                options.RunbooksPath,
                builder.Environment.ContentRootPath);
        }
    })
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.RunbooksPath),
        "Knowledge:RunbooksPath must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<EmbeddingServiceOptions>()
    .Bind(builder.Configuration.GetSection(
        EmbeddingServiceOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(
            options.BaseUrl,
            UriKind.Absolute,
            out _),
        "EmbeddingService:BaseUrl must be an absolute URL.")
    .ValidateOnStart();

builder.Services.AddHttpClient(
    FastApiEmbeddingClient.HttpClientName,
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<EmbeddingServiceOptions>>()
            .Value;

        client.BaseAddress = new Uri(
            $"{options.BaseUrl.TrimEnd('/')}/",
            UriKind.Absolute);
    });

builder.Services.AddTransient<IEmbeddingClient, FastApiEmbeddingClient>();

builder.Services.AddDbContext<IncidentCopilotDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.UseVector()));

builder.Services.AddScoped<
    IKnowledgeDocumentRepository,
    KnowledgeDocumentRepository>();
builder.Services.AddScoped<KnowledgeRetriever>();

builder.Services
    .AddOptions<TelemetrySimulationOptions>()
    .Bind(builder.Configuration.GetSection(
        TelemetrySimulationOptions.SectionName))
    .Validate(
        options => options.Interval > TimeSpan.Zero,
        "The telemetry simulation interval must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddSingleton<ITelemetryGenerator, TelemetryGenerator>();
builder.Services.AddSingleton<ISimulationScenarioState, SimulationScenarioState>();
builder.Services.AddHostedService<TelemetrySimulationService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<DevelopmentToolFaultInjector>();
    builder.Services.AddSingleton<IToolFaultInjector>(serviceProvider =>
        serviceProvider.GetRequiredService<DevelopmentToolFaultInjector>());
    builder.Services.AddSingleton<IToolFaultState>(serviceProvider =>
        serviceProvider.GetRequiredService<DevelopmentToolFaultInjector>());

}
else
{
    builder.Services.AddSingleton<IToolFaultInjector, NoToolFaultInjector>();
}

builder.Services.AddScoped<IKnowledgeRetriever>(serviceProvider =>
{
    IKnowledgeRetriever retriever = new InstrumentedKnowledgeRetriever(
        serviceProvider.GetRequiredService<KnowledgeRetriever>());

    if (builder.Environment.IsDevelopment())
    {
        retriever = new DevelopmentSecurityKnowledgeRetriever(
            retriever,
            serviceProvider.GetRequiredService<IToolFaultInjector>(),
            serviceProvider.GetRequiredService<IHostEnvironment>());
    }

    return retriever;
});
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapSimulationEndpoints(app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    app.MapToolCallingEvalEndpoints();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<IncidentCopilotDbContext>();

    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


app.Run();
