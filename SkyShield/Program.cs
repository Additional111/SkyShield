
using SkyShield.Services.Ingestion;
using SkyShield.Services.AircraftState;
using SkyShield.Services.Detection;
using SkyShield.Services.RiskScoring;
using SkyShield.Services.Demo;
using SkyShield.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

// Ingestion
builder.Services.AddScoped<AircraftValidationService>();

// Aircraft state
builder.Services.AddSingleton<IAircraftStateService, AircraftStateService>();

// Detection engine
builder.Services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();

builder.Services.AddSingleton<IDetectionRule, GhostAircraftRule>();
builder.Services.AddSingleton<IDetectionRule, ImpossibleMovementRule>();
builder.Services.AddSingleton<IDetectionRule, AltitudeJumpRule>();
builder.Services.AddSingleton<IDetectionRule, IdentityDuplicationRule>();

// Risk scoring
builder.Services.AddSingleton<IRiskScoringService, RiskScoringService>();

// Demo Mode
builder.Services.AddSingleton<DemoAircraftGenerator>();
builder.Services.AddSingleton<DemoScenarioService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DemoMovementService>();
    builder.Services.AddHostedService<DemoAlertService>();
}

var app = builder.Build();

// Initialize demo traffic in Development
if (app.Environment.IsDevelopment())
{
    app.Services
        .GetRequiredService<DemoScenarioService>()
        .Initialize();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();
app.MapHub<SkyShieldHub>("/hubs/skyshield");

app.Run();
