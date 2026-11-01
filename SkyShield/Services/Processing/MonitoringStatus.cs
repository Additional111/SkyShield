using SkyShield.Models;
using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Processing;

/// <summary>Tracks whether normal monitoring is alive and builds the status payload.</summary>
public class MonitoringStatus
{
    private readonly IAircraftStateService _state;
    private long _lastTickTicks;

    public MonitoringStatus(IAircraftStateService state) => _state = state;

    public int SimulatedAircraftCount { get; set; }

    public void MarkTick() => Interlocked.Exchange(ref _lastTickTicks, DateTimeOffset.UtcNow.UtcTicks);

    public SystemStatusDto Build()
    {
        var last = new DateTimeOffset(Interlocked.Read(ref _lastTickTicks), TimeSpan.Zero);
        bool active = _lastTickTicks != 0 && DateTimeOffset.UtcNow - last < TimeSpan.FromSeconds(5);

        return new SystemStatusDto(
            active,
            _state.GetAllStates().Count,
            SimulatedAircraftCount,
            DateTimeOffset.UtcNow);
    }
}
