using SkyShield.Models;

namespace SkyShield.Services.Demo;

/// <summary>Creates the synthetic fleet used for normal monitoring.</summary>
public class DemoAircraftGenerator
{
    public const int AircraftCount = 50;

    // Demonstration area (Romania and surroundings).
    public const double MinLatitude = 43.6, MaxLatitude = 48.0;
    public const double MinLongitude = 20.6, MaxLongitude = 29.0;

    private readonly Random _random = new();

    /// <summary>True for ICAOs produced by this generator (D00000..D00031).</summary>
    public static bool IsDemoAircraft(string icao) =>
        icao.Length == 6 && icao[0] == 'D' &&
        int.TryParse(icao.AsSpan(1), System.Globalization.NumberStyles.HexNumber, null, out int index) &&
        index < AircraftCount;

    public IReadOnlyList<AircraftMessage> GenerateAircraft()
    {
        var aircraft = new List<AircraftMessage>(AircraftCount);

        for (int i = 0; i < AircraftCount; i++)
        {
            aircraft.Add(new AircraftMessage
            {
                Icao = $"D{i:X5}",
                Latitude = MinLatitude + _random.NextDouble() * (MaxLatitude - MinLatitude),
                Longitude = MinLongitude + _random.NextDouble() * (MaxLongitude - MinLongitude),
                AltitudeFeet = _random.Next(18000, 41000),
                GroundSpeedKnots = _random.Next(350, 520),
                TimestampUtc = DateTimeOffset.UtcNow
            });
        }

        return aircraft;
    }
}
