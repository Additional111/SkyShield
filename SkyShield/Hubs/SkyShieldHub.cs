using Microsoft.AspNetCore.SignalR;
using SkyShield.Services.Processing;

namespace SkyShield.Hubs;

/// <summary>
/// Server-to-dashboard channel. Events: AircraftUpdated, AlertRaised,
/// SystemStatus, SimulationReset.
/// </summary>
public class SkyShieldHub : Hub
{
    private readonly MonitoringStatus _status;

    public SkyShieldHub(MonitoringStatus status) => _status = status;

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("SystemStatus", _status.Build());
        await base.OnConnectedAsync();
    }
}
