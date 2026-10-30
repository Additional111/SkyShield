using System.Collections.Concurrent;

namespace SkyShield.Services.AircraftState;

public class AircraftStateService : IAircraftStateService
{
    private readonly ConcurrentDictionary<string, AircraftStateEntry> _aircraft = new();

    public AircraftStateEntry? GetState(string icao)
    {
        _aircraft.TryGetValue(icao, out var state);

        return state;
    }

    public AircraftStateEntry? UpdateState(AircraftStateEntry state)
    {
        AircraftStateEntry? previousState = null;

        _aircraft.AddOrUpdate(
            state.Icao,
            state,
            (key, oldState) =>
            {
                previousState = oldState;
                return state;
            });

        return previousState;
    }
}