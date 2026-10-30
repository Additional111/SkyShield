using System.Text.Json.Serialization;

namespace SkyShield.Attacker.Cli.Models;

public class FlightTelemetryDto
{
    [JsonPropertyName("icao")]
    public string Icao { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("altitudeFeet")]
    public int AltitudeFeet { get; set; }

    [JsonPropertyName("groundSpeedKnots")]
    public int GroundSpeedKnots { get; set; }

    [JsonPropertyName("timestampUtc")]
    public string TimestampUtc { get; set; } = string.Empty;
}