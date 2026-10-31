using SkyShield.Hubs;
using SkyShield.Services.Ingestion;
using SkyShield.Services.AircraftState;
using SkyShield.Services.Detection;
using SkyShield.Services.RiskScoring;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SignalR
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

// SignalR Hub endpoint
app.MapHub<SkyShieldHub>("/hubs/skyshield");

app.Run();