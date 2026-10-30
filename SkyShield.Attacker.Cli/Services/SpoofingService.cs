using SkyShield.Attacker.Cli.Models;

namespace SkyShield.Attacker.Cli.Services;

public class SpoofingService
{
    public FlightTelemetryDto GenerateBaseTelemetry(string icao)
    {
        return new FlightTelemetryDto
        {
            Icao = icao,
            Latitude = 44.5711, 
            Longitude = 26.1010,
            AltitudeFeet = 32000,
            GroundSpeedKnots = 450,
            TimestampUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }
}