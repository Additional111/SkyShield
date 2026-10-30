namespace SkyShield.Services.AircraftState;

public class AircraftStateEntry
{
    public string Icao { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double AltitudeFeet { get; set; }

    public double GroundSpeedKnots { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }
}