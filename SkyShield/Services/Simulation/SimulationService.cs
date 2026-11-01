using Microsoft.AspNetCore.SignalR;
using SkyShield.Hubs;
using SkyShield.Models;
using SkyShield.Services.AircraftState;
using SkyShield.Services.Processing;

namespace SkyShield.Services.Simulation;

/// <summary>
/// Handles synthetic test events submitted through the simulation endpoint.
/// They run through the real detection/risk pipeline but against a PRIVATE state
/// store, so they can never corrupt the state of normal traffic.
/// </summary>
public class SimulationService
{
    private readonly AircraftStateService _simState = new();
    private readonly HashSet<string> _ghosts = new();
    private readonly object _ghostLock = new();

    private readonly TelemetryProcessor _processor;
    private readonly IAircraftStateService _mainState;
    private readonly MonitoringStatus _status;
    private readonly IHubContext<SkyShieldHub> _hub;

    public SimulationService(
        TelemetryProcessor processor,
        IAircraftStateService mainState,
        MonitoringStatus status,
        IHubContext<SkyShieldHub> hub,
        IConfiguration configuration)
    {
        _processor = processor;
        _mainState = mainState;
        _status = status;
        _hub = hub;
        Enabled = configuration.GetValue("Simulation:Enabled", true);
    }

    public bool Enabled { get; }

    public int GhostCount
    {
        get { lock (_ghostLock) return _ghosts.Count; }
    }

    public async Task<ProcessingResult> SubmitAsync(AircraftMessage message, CancellationToken ct)
    {
        string icao = message.Icao.Trim().ToUpperInvariant();

        // An aircraft that is not part of normal traffic is shown on the map as a
        // temporary marker. Messages for real fleet aircraft are NOT broadcast as
        // positions, so a spoofed position never moves a real marker.
        bool temporaryAircraft = _mainState.GetState(icao) == null;

        if (temporaryAircraft)
        {
            lock (_ghostLock) _ghosts.Add(icao);
            _status.SimulatedAircraftCount = GhostCount;
        }

        return await _processor.ProcessAsync(message, _simState, temporaryAircraft, ct);
    }

    /// <summary>Clears simulation state. With broadcast, dashboards also drop temporary markers and alerts.</summary>
    public async Task ResetAsync(bool broadcast, CancellationToken ct)
    {
        _simState.Clear();

        lock (_ghostLock) _ghosts.Clear();
        _status.SimulatedAircraftCount = 0;

        if (broadcast)
            await _hub.Clients.All.SendAsync("SimulationReset", new { clearAlerts = true }, ct);
    }
}
