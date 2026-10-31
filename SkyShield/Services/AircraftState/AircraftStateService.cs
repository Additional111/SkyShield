using System.Collections.Concurrent;
using SkyShield.Models;

namespace SkyShield.Services.AircraftState;

public class AircraftStateService : IAircraftStateService
{
    private readonly ConcurrentDictionary<string, AircraftMessage> _aircraft = new();

    private readonly ConcurrentDictionary<string, object> _locks = new();

    public AircraftMessage? GetState(string icao)
    {
        var lockObject = _locks.GetOrAdd(icao, _ => new object());

        lock (lockObject)
        {
            if (_aircraft.TryGetValue(icao, out var message))
            {
                return CopyMessage(message);
            }

            return null;
        }
    }

    public AircraftMessage? UpdateState(AircraftMessage message)
    {
        var lockObject = _locks.GetOrAdd(message.Icao, _ => new object());

        lock (lockObject)
        {
            _aircraft.TryGetValue(message.Icao, out var previousState);

            _aircraft[message.Icao] = CopyMessage(message);

            return previousState == null ? null : CopyMessage(previousState);
        }
    }

    public IReadOnlyList<AircraftMessage> GetAllStates()
    {
        return _aircraft.Values
            .Select(CopyMessage)
            .ToList();
    }

    private static AircraftMessage CopyMessage(AircraftMessage message)
    {
        return new AircraftMessage
        {
            Icao = message.Icao,
            Latitude = message.Latitude,
            Longitude = message.Longitude,
            AltitudeFeet = message.AltitudeFeet,
            GroundSpeedKnots = message.GroundSpeedKnots,
            TimestampUtc = message.TimestampUtc
        };
    }
}