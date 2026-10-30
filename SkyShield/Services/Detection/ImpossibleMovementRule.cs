
using System.Globalization;
using Microsoft.Extensions.Configuration;
using SkyShield.Models;

namespace SkyShield.Services.Detection;

public class ImpossibleMovementRule : IDetectionRule
{
    private const double DefaultMaxSpeedKnots = 1200.0;

    private readonly double _maxSpeedKnots;

    public ImpossibleMovementRule(IConfiguration configuration)
    {
        string? configuredValue =
            configuration["Detection:MaxMovementSpeedKnots"];

        if (double.TryParse(
                configuredValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double configuredSpeed) &&
            double.IsFinite(configuredSpeed) &&
            configuredSpeed > 0)
        {
            _maxSpeedKnots = configuredSpeed;
        }
        else
        {
            _maxSpeedKnots = DefaultMaxSpeedKnots;
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

        if (!IsValidCoordinates(message) ||
            !IsValidCoordinates(previousState))
            return null;

        double elapsedHours =
            (message.TimestampUtc - previousState.TimestampUtc)
            .TotalHours;

        if (elapsedHours <= 0)
            return null;

        double distanceKm = HaversineCalculator.CalculateDistance(
            previousState.Latitude,
            previousState.Longitude,
            message.Latitude,
            message.Longitude);

        double speedKnots = distanceKm / elapsedHours / 1.852;

        if (!double.IsFinite(speedKnots) ||
            speedKnots <= _maxSpeedKnots)
            return null;

        return new AnomalyAlert
        {
            Icao = message.Icao,
            AnomalyType = "Impossible Movement",
            Severity = "Critical",
            RiskScore = 70,
            Description =
                $"Aircraft moved at {speedKnots:F2} knots, " +
                $"exceeding the maximum allowed speed of {_maxSpeedKnots:F2} knots.",
            DetectedAtUtc = message.TimestampUtc,
            Latitude = message.Latitude,
            Longitude = message.Longitude
        };
    }

    private static bool IsValidCoordinates(AircraftMessage message)
    {
        return double.IsFinite(message.Latitude) &&
               double.IsFinite(message.Longitude) &&
               message.Latitude >= -90 &&
               message.Latitude <= 90 &&
               message.Longitude >= -180 &&
               message.Longitude <= 180;
    }
}
