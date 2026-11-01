using SkyShield.Attacker.Cli.Models;

namespace SkyShield.Attacker.Cli.Scenarios;

public static class ScenarioCatalog
{
    public static readonly IReadOnlyList<IScenario> All = new IScenario[]
    {
        new GhostAircraftScenario(),
        new UnusualMovementScenario(),
        new AltitudeAnomalyScenario(),
        new DuplicateIdentityScenario()
    };

    public static IScenario? Find(string name) =>
        All.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            s.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)));
}

internal static class Synthetic
{
    /// <summary>Copy of the baseline shifted in time and position. Timestamps are synthetic.</summary>
    public static TelemetryDto At(
        TelemetryDto b, int secondsFromStart, double dLat = 0, double dLon = 0, double? altitude = null)
    {
        return new TelemetryDto
        {
            Icao = b.Icao,
            Latitude = b.Latitude + dLat,
            Longitude = b.Longitude + dLon,
            AltitudeFeet = altitude ?? b.AltitudeFeet,
            GroundSpeedKnots = b.GroundSpeedKnots,
            TimestampUtc = DateTimeOffset.UtcNow.AddSeconds(secondsFromStart - 60)
        };
    }
}

public class GhostAircraftScenario : IScenario
{
    public string Name => "ghost-aircraft";
    public string[] Aliases => new[] { "ghost" };
    public string Description => "An unknown ICAO transmits telemetry although it is not in the authorized fleet.";
    public bool UsesFleetAircraft => false;

    public IReadOnlyList<TelemetryDto> Build(TelemetryDto baseline) => new[]
    {
        Synthetic.At(new TelemetryDto
        {
            Icao = baseline.Icao,
            Latitude = baseline.Latitude,
            Longitude = baseline.Longitude,
            AltitudeFeet = 30000,
            GroundSpeedKnots = 420
        }, 60)
    };
}

public class UnusualMovementScenario : IScenario
{
    public string Name => "unusual-movement";
    public string[] Aliases => new[] { "teleport", "impossible-movement" };
    public string Description => "A known aircraft jumps ~2200 km in 5 seconds (impossible ground speed).";
    public bool UsesFleetAircraft => true;

    public IReadOnlyList<TelemetryDto> Build(TelemetryDto b) => new[]
    {
        Synthetic.At(b, 0),
        Synthetic.At(b, 5, dLat: b.Latitude > 0 ? -20 : 20)
    };
}

public class AltitudeAnomalyScenario : IScenario
{
    public string Name => "altitude-anomaly";
    public string[] Aliases => new[] { "altitude-jump" };
    public string Description => "A known aircraft changes altitude by 18,000 ft in 3 seconds.";
    public bool UsesFleetAircraft => true;

    public IReadOnlyList<TelemetryDto> Build(TelemetryDto b) => new[]
    {
        Synthetic.At(b, 0),
        Synthetic.At(b, 3, altitude: b.AltitudeFeet >= 20000 ? b.AltitudeFeet - 18000 : b.AltitudeFeet + 18000)
    };
}

public class DuplicateIdentityScenario : IScenario
{
    public string Name => "duplicate-identity";
    public string[] Aliases => new[] { "identity-spoof", "identity-duplication" };
    public string Description => "The same ICAO is reported alternately from two places ~270 km apart.";
    public bool UsesFleetAircraft => true;

    public IReadOnlyList<TelemetryDto> Build(TelemetryDto b) => new[]
    {
        Synthetic.At(b, 0),
        Synthetic.At(b, 3, dLat: 2.0, dLon: -1.5),
        Synthetic.At(b, 6)
    };
}
