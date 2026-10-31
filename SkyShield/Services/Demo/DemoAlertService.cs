
using Microsoft.AspNetCore.SignalR;
using SkyShield.Hubs;
using SkyShield.Models;
using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Demo;

public class DemoAlertService : BackgroundService
{
    private readonly IAircraftStateService _aircraftStateService;
    private readonly IHubContext<SkyShieldHub> _hubContext;

    public DemoAlertService(
        IAircraftStateService aircraftStateService,
        IHubContext<SkyShieldHub> hubContext)
    {
        _aircraftStateService = aircraftStateService;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var demoAlerts = new[]
        {
            new
            {
                Type = "DemoTelemetryWarning",
                Severity = "Medium",
                RiskScore = 35,
                Description = "[DEMO] Synthetic telemetry warning."
            },
            new
            {
                Type = "DemoRouteDeviation",
                Severity = "High",
                RiskScore = 65,
                Description = "[DEMO] Synthetic route deviation."
            },
            new
            {
                Type = "DemoSensorInconsistency",
                Severity = "Critical",
                RiskScore = 85,
                Description = "[DEMO] Synthetic sensor inconsistency."
            }
        };

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(12));

        foreach (var demoAlert in demoAlerts)
        {
            await timer.WaitForNextTickAsync(stoppingToken);

            var aircraft = _aircraftStateService
                .GetAllStates()
                .FirstOrDefault();

            if (aircraft == null)
                continue;

            var alert = new AnomalyAlert
            {
                Id = Guid.NewGuid(),
                Icao = aircraft.Icao,
                AnomalyType = demoAlert.Type,
                Severity = demoAlert.Severity,
                RiskScore = demoAlert.RiskScore,
                Description = demoAlert.Description,
                DetectedAtUtc = DateTimeOffset.UtcNow,
                Latitude = aircraft.Latitude,
                Longitude = aircraft.Longitude
            };

            await _hubContext.Clients.All.SendAsync(
                "AlertRaised",
                alert,
                stoppingToken);

            Console.WriteLine(
                $"[DEMO] Alert raised: {demoAlert.Type}");
        }
    }
}
