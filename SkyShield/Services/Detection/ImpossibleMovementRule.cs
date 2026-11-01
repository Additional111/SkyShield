using System.Globalization;
using SkyShield.Models;

namespace SkyShield.Services.Detection;

/// <summary>Flags position jumps that would require an impossible ground speed.</summary>
public class ImpossibleMovementRule : IDetectionRule
{
    public const string Type = "Impossible Movement";

    private const double DefaultMaxSpeedKnots = 1200.0;
    private const double MinimumElapsedSeconds = 1.0;

    private readonly double _maxSpeedKnots;

    public ImpossibleMovementRule(IConfiguration configuration)
    {
        _maxSpeedKnots = double.TryParse(
            configuration["Detection:MaxMovementSpeedKnots"],
            NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0
            ? v : DefaultMaxSpeedKnots;
    }

    public AnomalyAlert? Detect(DetectionContext context)
    {
        var message = context.Message;
        var previous = context.Previous;

        if (previous == null)
            return null;

        double elapsedSeconds = (message.TimestampUtc - previous.TimestampUtc).TotalSeconds;

        // Out-of-order or near-simultaneous messages cannot give a meaningful speed.
        if (elapsedSeconds < MinimumElapsedSeconds)
            return null;

        double distanceKm = HaversineCalculator.CalculateDistance(
            previous.Latitude, previous.Longitude,
            message.Latitude, message.Longitude);

        double speedKnots = distanceKm / (elapsedSeconds / 3600.0) / 1.852;

        if (!double.IsFinite(speedKnots) || speedKnots <= _maxSpeedKnots)
            return null;

        return new AnomalyAlert
        {
            Icao = message.Icao,
            AnomalyType = Type,
            RiskScore = 85,
            Description =
                $"Moved {distanceKm:F0} km in {elapsedSeconds:F0} s " +
                $"({speedKnots:F0} kt, limit {_maxSpeedKnots:F0} kt).",
            DetectedAtUtc = DateTimeOffset.UtcNow,
            Latitude = message.Latitude,
            Longitude = message.Longitude
        };
    }
}
