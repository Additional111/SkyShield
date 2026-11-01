using System.Collections.Concurrent;

namespace SkyShield.Services.AircraftState;

/// <summary>
/// The set of aircraft the system knows and trusts: the configured
/// "AuthorizedAircraft" list plus the synthetic demo fleet.
/// Anything outside this set is considered a ghost aircraft.
/// </summary>
public class FleetRegistry
{
    private readonly ConcurrentDictionary<string, byte> _known = new();

    public FleetRegistry(IConfiguration configuration)
    {
        foreach (var child in configuration.GetSection("AuthorizedAircraft").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(child.Value))
                Register(child.Value);
        }
    }

    public void Register(string icao) =>
        _known[icao.Trim().ToUpperInvariant()] = 0;

    public bool IsKnown(string icao) =>
        _known.ContainsKey(icao.Trim().ToUpperInvariant());
}
