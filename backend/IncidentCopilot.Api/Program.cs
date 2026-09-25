using IncidentCopilot.Api.Endpoints;
using IncidentCopilot.Core.AI;
using IncidentCopilot.Infrastructure.AI;
using IncidentCopilot.Infrastructure.Persistence;
using IncidentCopilot.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddDbContext<IncidentCopilotDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));


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
