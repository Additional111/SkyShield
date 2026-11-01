using System.Collections.Concurrent;
using SkyShield.Models;

namespace SkyShield.Services.AircraftState;

/// <summary>
/// Thread-safe store with the latest state and a short history per aircraft.
/// Every instance is independent, so simulation state never mixes with real traffic.
/// </summary>
public class AircraftStateService : IAircraftStateService
{
    private const int MaxHistory = 20;

    private sealed class Track
    {
        public readonly object Lock = new();
        public AircraftMessage? Latest;
        public readonly List<AircraftMessage> History = new();
    }

    private readonly ConcurrentDictionary<string, Track> _tracks = new();

    public AircraftMessage? GetState(string icao)
    {
        if (!_tracks.TryGetValue(Normalize(icao), out var track))
            return null;

        lock (track.Lock)
        {
            return track.Latest == null ? null : Copy(track.Latest);
        }
    }

    public IReadOnlyList<AircraftMessage> GetAllStates()
    {
        var result = new List<AircraftMessage>();

        foreach (var track in _tracks.Values)
        {
            lock (track.Lock)
            {
                if (track.Latest != null)
                    result.Add(Copy(track.Latest));
            }
        }

        return result;
    }

    public StateUpdate Update(AircraftMessage message)
    {
        var copy = Copy(message);
        var track = _tracks.GetOrAdd(copy.Icao, _ => new Track());

        lock (track.Lock)
        {
            var previous = track.Latest == null ? null : Copy(track.Latest);
            var history = track.History.Select(Copy).ToList();

            // Late (out-of-order) messages never overwrite newer state.
            if (track.Latest == null || copy.TimestampUtc >= track.Latest.TimestampUtc)
            {
                track.Latest = copy;
                track.History.Add(Copy(copy));

                if (track.History.Count > MaxHistory)
                    track.History.RemoveAt(0);
            }

            return new StateUpdate(previous, history);
        }
    }

    public void Clear() => _tracks.Clear();

    private static string Normalize(string? icao) =>
        (icao ?? string.Empty).Trim().ToUpperInvariant();

    private static AircraftMessage Copy(AircraftMessage m) => new()
    {
        Icao = Normalize(m.Icao),
        Latitude = m.Latitude,
        Longitude = m.Longitude,
        AltitudeFeet = m.AltitudeFeet,
        GroundSpeedKnots = m.GroundSpeedKnots,
        TimestampUtc = m.TimestampUtc
    };
}
