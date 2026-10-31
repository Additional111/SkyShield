
using Microsoft.AspNetCore.SignalR;
using SkyShield.Hubs;
using SkyShield.Models;
using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Demo;

public class DemoMovementService : BackgroundService
{
    private readonly IAircraftStateService _aircraftStateService;
    private readonly IHubContext<SkyShieldHub> _hubContext;

    public DemoMovementService(
        IAircraftStateService aircraftStateService,
        IHubContext<SkyShieldHub> hubContext)
    {
        _aircraftStateService = aircraftStateService;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var aircraftList = _aircraftStateService.GetAllStates();

            foreach (var aircraft in aircraftList)
            {
                var direction = GetDirection(aircraft.Icao);

                var updatedAircraft = new AircraftMessage
                {
                    Icao = aircraft.Icao,
                    Latitude = aircraft.Latitude + direction.Latitude,
                    Longitude = aircraft.Longitude + direction.Longitude,
                    AltitudeFeet = aircraft.AltitudeFeet,
                    GroundSpeedKnots = aircraft.GroundSpeedKnots,
                    TimestampUtc = DateTimeOffset.UtcNow
                };

                _aircraftStateService.UpdateState(updatedAircraft);

                await _hubContext.Clients.All.SendAsync(
                    "AircraftUpdated",
                    updatedAircraft,
                    stoppingToken);
            }
        }
    }

    private static (double Latitude, double Longitude) GetDirection(
        string icao)
    {
        var hash = icao.GetHashCode();

        var latitudeDirection = (hash & 1) == 0 ? 1 : -1;
        var longitudeDirection = (hash & 2) == 0 ? 1 : -1;

        return (
            latitudeDirection * 0.0002,
            longitudeDirection * 0.0002
        );
    }
}
