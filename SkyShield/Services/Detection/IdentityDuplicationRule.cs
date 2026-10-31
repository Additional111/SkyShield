
using System.Globalization;
using Microsoft.Extensions.Configuration;
using SkyShield.Models;

namespace SkyShield.Services.Detection;

public class IdentityDuplicationRule : IDetectionRule
{
    private const double DefaultDistanceKm = 100.0;
    private const double DefaultTimeWindowSeconds = 60.0;

    private readonly double _minimumDistanceKm;
    private readonly double _timeWindowSeconds;

    public IdentityDuplicationRule(IConfiguration configuration)
    {
        _minimumDistanceKm = ReadPositiveValue(
            configuration["Detection:IdentityDuplicationDistanceKm"],
            DefaultDistanceKm);

        _timeWindowSeconds = ReadPositiveValue(
            configuration["Detection:IdentityDuplicationWindowSeconds"],
            DefaultTimeWindowSeconds);
    }

    public AnomalyAlert? Detect(
        AircraftMessage message,
        AircraftMessage? previousState)
    {
        if (previousState == null)
            return null;

        if (string.IsNullOrWhiteSpace(message.Icao))
            return null;

        if (message.TimestampUtc == default ||
            previousState.TimestampUtc == default)
            return null;

        if (!IsValidCoordinates(message) ||
            !IsValidCoordinates(previousState))
            return null;

        double timeDifference = Math.Abs(
            (message.TimestampUtc - previousState.TimestampUtc)
            .TotalSeconds);

        if (timeDifference > _timeWindowSeconds)
            return null;

        double distanceKm = HaversineCalculator.CalculateDistance(
            previousState.Latitude,
            previousState.Longitude,
            message.Latitude,
            message.Longitude);

        if (!double.IsFinite(distanceKm) ||
            distanceKm < _minimumDistanceKm)
            return null;

        return new AnomalyAlert
        {
            Icao = message.Icao,
            AnomalyType = "Identity Duplication",
            Severity = "Critical",
            RiskScore = 45,
            Description =
                $"The same ICAO was reported from positions " +
                $"{distanceKm:F2} km apart within {timeDifference:F2} seconds. " +
                "Possible identity duplication.",
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

    private static double ReadPositiveValue(
        string? value,
        double defaultValue)
    {
        if (double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double result) &&
            double.IsFinite(result) &&
            result > 0)
        {
            return result;
        }

        return defaultValue;
    }
}
