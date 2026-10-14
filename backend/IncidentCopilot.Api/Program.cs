using IncidentCopilot.Api.Endpoints;
using IncidentCopilot.Application.AI.Tools;
using IncidentCopilot.Application.Chat;
using IncidentCopilot.Application.Knowledge;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.AI;
using IncidentCopilot.Infrastructure.AI.Tools.GetRecentLogs;
using IncidentCopilot.Infrastructure.AI.Tools.GetServiceMetrics;
using IncidentCopilot.Infrastructure.Persistence;
using IncidentCopilot.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen();

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

builder.Services.AddHttpClient<ILlmClient, GroqLlmClient>();
builder.Services.AddScoped<IAgentTool, GetServiceMetricsTool>();
builder.Services.AddScoped<IAgentTool, GetRecentLogsTool>();
builder.Services.AddSingleton<IMarkdownChunker, MarkdownChunker>();
builder.Services.AddScoped<IKnowledgeIndexer, KnowledgeIndexer>();
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services
    .AddOptions<ChatDiagnosticsOptions>()
    .Bind(builder.Configuration.GetSection(
        ChatDiagnosticsOptions.SectionName))
    .Validate(
        options => options.ChunkContentLimit > 0,
        "ChatDiagnostics:ChunkContentLimit must be greater than zero.")
    .ValidateOnStart();

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
builder.Services.AddScoped<IKnowledgeRetriever, KnowledgeRetriever>();


if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddOptions<TelemetrySimulationOptions>()
        .Bind(builder.Configuration.GetSection(
            TelemetrySimulationOptions.SectionName))
        .Validate(
            options => options.Interval > TimeSpan.Zero,
            "The telemetry simulation interval must be greater than zero.")
        .ValidateOnStart();

    builder.Services.AddSingleton<
        ITelemetryGenerator,
        TelemetryGenerator>();

    builder.Services.AddSingleton<
        ISimulationScenarioState,
        SimulationScenarioState>();

    builder.Services.AddHostedService<
        TelemetrySimulationService>();
}
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapSimulationEndpoints();
    app.MapToolCallingEvalEndpoints();
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<IncidentCopilotDbContext>();

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
