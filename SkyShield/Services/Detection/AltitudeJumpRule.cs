using System.Globalization;
using SkyShield.Models;

namespace SkyShield.Services.Detection;

/// <summary>Flags altitude changes faster than any aircraft can climb or descend.</summary>
public class AltitudeJumpRule : IDetectionRule
{
    public const string Type = "Altitude Jump";

    private const double DefaultMaxFeetPerMinute = 10000.0;
    private const double MinimumElapsedSeconds = 1.0;

    private readonly double _maxFeetPerMinute;

    public AltitudeJumpRule(IConfiguration configuration)
    {
        _maxFeetPerMinute = double.TryParse(
            configuration["Detection:MaxAltitudeChangeFeetPerMinute"],
            NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0
            ? v : DefaultMaxFeetPerMinute;
    }

    public AnomalyAlert? Detect(DetectionContext context)
    {
        var message = context.Message;
        var previous = context.Previous;

        if (previous == null)
            return null;

        double elapsedSeconds = (message.TimestampUtc - previous.TimestampUtc).TotalSeconds;

        if (elapsedSeconds < MinimumElapsedSeconds)
            return null;

        double change = Math.Abs(message.AltitudeFeet - previous.AltitudeFeet);
        double feetPerMinute = change / (elapsedSeconds / 60.0);

        if (!double.IsFinite(feetPerMinute) || feetPerMinute <= _maxFeetPerMinute)
            return null;

        return new AnomalyAlert
        {
            Icao = message.Icao,
            AnomalyType = Type,
            RiskScore = 65,
            Description =
                $"Altitude changed by {change:F0} ft in {elapsedSeconds:F0} s " +
                $"({feetPerMinute:F0} ft/min, limit {_maxFeetPerMinute:F0} ft/min).",
            DetectedAtUtc = DateTimeOffset.UtcNow,
            Latitude = message.Latitude,
            Longitude = message.Longitude
        };
    }
}
