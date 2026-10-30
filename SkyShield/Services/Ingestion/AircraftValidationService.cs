using SkyShield.Models;

namespace SkyShield.Services.Ingestion;

public class AircraftValidationService
{
    public bool IsValid(AircraftMessage message, out string error)
    {
        error = string.Empty;

        if (message == null)
        {
            error = "Aircraft message cannot be null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.Icao) ||
            message.Icao.Length != 6 ||
            !message.Icao.All(Uri.IsHexDigit))
        {
            error = "Invalid ICAO identifier.";
            return false;
        }

        if (message.Latitude < -90 || message.Latitude > 90)
        {
            error = "Invalid latitude.";
            return false;
        }

        if (message.Longitude < -180 || message.Longitude > 180)
        {
            error = "Invalid longitude.";
            return false;
        }

        if (message.AltitudeFeet < 0)
        {
            error = "Invalid altitude.";
            return false;
        }

        if (message.GroundSpeedKnots < 0)
        {
            error = "Invalid ground speed.";
            return false;
        }

        if (message.TimestampUtc == default ||
            message.TimestampUtc > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            error = "Invalid timestamp.";
            return false;
        }

        return true;
    }
}