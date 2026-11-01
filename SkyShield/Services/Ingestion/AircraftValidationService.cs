using SkyShield.Models;

namespace SkyShield.Services.Ingestion;

public class AircraftValidationService
{
    public bool IsValid(AircraftMessage? message, out string error)
    {
        error = string.Empty;

        if (message == null)
        {
            error = "Aircraft message cannot be null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.Icao) ||
            message.Icao.Trim().Length != 6 ||
            !message.Icao.Trim().All(Uri.IsHexDigit))
        {
            error = "Invalid ICAO identifier: expected 6 hexadecimal characters.";
            return false;
        }

        if (!double.IsFinite(message.Latitude) || message.Latitude < -90 || message.Latitude > 90)
        {
            error = "Invalid latitude: expected a value between -90 and 90.";
            return false;
        }

        if (!double.IsFinite(message.Longitude) || message.Longitude < -180 || message.Longitude > 180)
        {
            error = "Invalid longitude: expected a value between -180 and 180.";
            return false;
        }

        if (!double.IsFinite(message.AltitudeFeet) || message.AltitudeFeet < 0 || message.AltitudeFeet > 100000)
        {
            error = "Invalid altitude: expected 0-100000 feet.";
            return false;
        }

        if (!double.IsFinite(message.GroundSpeedKnots) || message.GroundSpeedKnots < 0 || message.GroundSpeedKnots > 5000)
        {
            error = "Invalid ground speed: expected 0-5000 knots.";
            return false;
        }

        if (message.TimestampUtc == default ||
            message.TimestampUtc > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            error = "Invalid timestamp: missing or in the future.";
            return false;
        }

        return true;
    }
}
