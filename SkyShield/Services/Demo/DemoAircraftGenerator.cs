using SkyShield.Models;

namespace SkyShield.Services.Demo;

public class DemoAircraftGenerator
{
    private const int AircraftCount = 50;

    private readonly Random _random = new();

    public IReadOnlyList<AircraftMessage> GenerateAircraft()
    {
        var aircraft = new List<AircraftMessage>();

        for (int i = 0; i < AircraftCount; i++)
        {
            aircraft.Add(new AircraftMessage
            {
                Icao = GenerateIcao(i),
                Latitude = 43.5 + _random.NextDouble() * 4.5,
                Longitude = 20.5 + _random.NextDouble() * 8.5,
                AltitudeFeet = _random.Next(18000, 41000),
                GroundSpeedKnots = _random.Next(350, 520),
                TimestampUtc = DateTimeOffset.UtcNow
            });
        }

        return aircraft;
    }

    private static string GenerateIcao(int index)
    {
        return $"D{index:X5}";
    }
}