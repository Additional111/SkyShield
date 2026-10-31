
using System.Globalization;
using Microsoft.Extensions.Configuration;
using SkyShield.Models;

namespace SkyShield.Services.Detection;

public class AltitudeJumpRule : IDetectionRule
{
    private const double DefaultMaxVerticalSpeed = 10000.0;

    private readonly double _maxVerticalSpeed;

    public AltitudeJumpRule(IConfiguration configuration)
    {
        string? configuredValue =
            configuration["Detection:MaxAltitudeChangeFeetPerMinute"];

        if (double.TryParse(
                configuredValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double configuredSpeed) &&
            double.IsFinite(configuredSpeed) &&
            configuredSpeed > 0)
        {
            _maxVerticalSpeed = configuredSpeed;
        }
        else
        {
            _maxVerticalSpeed = DefaultMaxVerticalSpeed;
        }
    }

    public AnomalyAlert? Detect(
        AircraftMessage message,
        AircraftMessage? previousState)
    {
        if (previousState == null)
            return null;

        if (message.TimestampUtc == default ||
            previousState.TimestampUtc == default)
            return null;

        if (!double.IsFinite(message.AltitudeFeet) ||
            !double.IsFinite(previousState.AltitudeFeet))
            return null;

        double elapsedMinutes =
            (message.TimestampUtc - previousState.TimestampUtc)
            .TotalMinutes;

        if (elapsedMinutes <= 0)
            return null;

        double altitudeDifference = Math.Abs(
            message.AltitudeFeet - previousState.AltitudeFeet);

        double verticalSpeed = altitudeDifference / elapsedMinutes;

        if (!double.IsFinite(verticalSpeed) ||
            verticalSpeed <= _maxVerticalSpeed)
            return null;

        return new AnomalyAlert
        {
            Icao = message.Icao,
            AnomalyType = "Altitude Jump",
            Severity = "Critical",
            RiskScore = 70,
            Description =
                $"Aircraft altitude changed by {altitudeDifference:F2} feet " +
                $"at a rate of {verticalSpeed:F2} feet per minute.",
            DetectedAtUtc = message.TimestampUtc,
            Latitude = message.Latitude,
            Longitude = message.Longitude
        };
    }
}
