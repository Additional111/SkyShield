using Microsoft.AspNetCore.SignalR;
using SkyShield.Hubs;
using SkyShield.Models;
using SkyShield.Services.AircraftState;
using SkyShield.Services.Processing;

namespace SkyShield.Services.Demo;

/// <summary>
/// Normal traffic: moves the synthetic fleet along its heading and broadcasts
/// positions. This feed is a trusted simulator and deliberately independent of the
/// manual scenario system. Aircraft move "Demo:TimeScale" times faster than real
/// time so the motion is visible on the map; it never produces alerts.
/// </summary>
public class DemoMovementService : BackgroundService
{
    private const double KnotsToDegreesLatPerSecond = 1.0 / 3600.0 / 60.0; // 1 kt = 1 nm/h, 60 nm = 1 deg

    private readonly IAircraftStateService _state;
    private readonly IHubContext<SkyShieldHub> _hub;
    private readonly MonitoringStatus _status;
    private readonly double _timeScale;
    private readonly Dictionary<string, double> _headings = new();
    private readonly Random _random = new();

    public DemoMovementService(
        IAircraftStateService state,
        IHubContext<SkyShieldHub> hub,
        MonitoringStatus status,
        IConfiguration configuration)
    {
        _state = state;
        _hub = hub;
        _status = status;
        _timeScale = Math.Max(1, configuration.GetValue("Demo:TimeScale", 8.0));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DEMO] Movement tick failed: {ex.Message}");
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var aircraft in _state.GetAllStates())
        {
            if (!DemoAircraftGenerator.IsDemoAircraft(aircraft.Icao))
                continue; // only synthetic fleet members are moved here

            double seconds = Math.Clamp((now - aircraft.TimestampUtc).TotalSeconds, 0, 5) * _timeScale;

            if (!_headings.TryGetValue(aircraft.Icao, out double heading))
                heading = _random.NextDouble() * 360;

            heading = (heading + (_random.NextDouble() - 0.5) * 2) % 360;

            double distanceDeg = aircraft.GroundSpeedKnots * KnotsToDegreesLatPerSecond * seconds;
            double rad = heading * Math.PI / 180;
            double cosLat = Math.Max(0.2, Math.Cos(aircraft.Latitude * Math.PI / 180));

            double lat = aircraft.Latitude + distanceDeg * Math.Cos(rad);
            double lon = aircraft.Longitude + distanceDeg * Math.Sin(rad) / cosLat;

            // Turn around when leaving the demonstration area.
            if (lat < DemoAircraftGenerator.MinLatitude || lat > DemoAircraftGenerator.MaxLatitude ||
                lon < DemoAircraftGenerator.MinLongitude || lon > DemoAircraftGenerator.MaxLongitude)
            {
                heading = (heading + 180) % 360;
                lat = Math.Clamp(lat, DemoAircraftGenerator.MinLatitude, DemoAircraftGenerator.MaxLatitude);
                lon = Math.Clamp(lon, DemoAircraftGenerator.MinLongitude, DemoAircraftGenerator.MaxLongitude);
            }

            _headings[aircraft.Icao] = heading;

            var updated = new AircraftMessage
            {
                Icao = aircraft.Icao,
                Latitude = lat,
                Longitude = lon,
                AltitudeFeet = aircraft.AltitudeFeet,
                GroundSpeedKnots = aircraft.GroundSpeedKnots,
                TimestampUtc = now
            };

            _state.Update(updated);
            await _hub.Clients.All.SendAsync("AircraftUpdated", updated, ct);
        }

        _status.MarkTick();
        await _hub.Clients.All.SendAsync("SystemStatus", _status.Build(), ct);
    }
}
