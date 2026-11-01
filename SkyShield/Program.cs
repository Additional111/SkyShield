using SkyShield.Hubs;
using SkyShield.Services.AircraftState;
using SkyShield.Services.Demo;
using SkyShield.Services.Detection;
using SkyShield.Services.Ingestion;
using SkyShield.Services.Processing;
using SkyShield.Services.RiskScoring;
using SkyShield.Services.Simulation;

var builder = WebApplication.CreateBuilder(args);

// Default address (matches the CLI); override with --urls or ASPNETCORE_URLS.
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
    builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Ingestion and state
builder.Services.AddSingleton<AircraftValidationService>();
builder.Services.AddSingleton<IAircraftStateService, AircraftStateService>();
builder.Services.AddSingleton<FleetRegistry>();

// Detection engine: each rule is independent; add or remove lines to change behaviour.
builder.Services.AddSingleton<IDetectionRule, GhostAircraftRule>();
builder.Services.AddSingleton<IDetectionRule, ImpossibleMovementRule>();
builder.Services.AddSingleton<IDetectionRule, AltitudeJumpRule>();
builder.Services.AddSingleton<IDetectionRule, IdentityDuplicationRule>();
builder.Services.AddSingleton<IAnomalyDetectionService, AnomalyDetectionService>();

// Risk scoring and the shared processing pipeline
builder.Services.AddSingleton<IRiskScoringService, RiskScoringService>();
builder.Services.AddSingleton<TelemetryProcessor>();
builder.Services.AddSingleton<MonitoringStatus>();

// Manual simulation (driven only by SkyShield.Attacker.Cli through the API)
builder.Services.AddSingleton<SimulationService>();

// Normal synthetic traffic (independent of simulation)
builder.Services.AddSingleton<DemoAircraftGenerator>();
builder.Services.AddSingleton<DemoTrafficInitializer>();

bool demoEnabled = builder.Configuration.GetValue("Demo:Enabled", true);
if (demoEnabled)
    builder.Services.AddHostedService<DemoMovementService>();

var app = builder.Build();

if (demoEnabled)
    app.Services.GetRequiredService<DemoTrafficInitializer>().Initialize();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<SkyShieldHub>("/hubs/skyshield");

app.Run();
