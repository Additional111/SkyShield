using System.Globalization;
using SkyShield.Models;

namespace SkyShield.Services.Detection;

/// <summary>
/// Flags one ICAO that alternates between two distant positions within a short
/// window (A, B, A). A one-way jump is NOT duplication: that is an impossible
/// movement and is handled by <see cref="ImpossibleMovementRule"/>.
/// </summary>
public class IdentityDuplicationRule : IDetectionRule
{
    public const string Type = "Identity Duplication";

    private const double DefaultDistanceKm = 100.0;
    private const double DefaultWindowSeconds = 60.0;

    private readonly double _minimumDistanceKm;
    private readonly double _windowSeconds;

    public IdentityDuplicationRule(IConfiguration configuration)
    {
        _minimumDistanceKm = ReadPositive(configuration["Detection:IdentityDuplicationDistanceKm"], DefaultDistanceKm);
        _windowSeconds = ReadPositive(configuration["Detection:IdentityDuplicationWindowSeconds"], DefaultWindowSeconds);
    }

    public IReadOnlyCollection<string> Supersedes { get; } = new[] { ImpossibleMovementRule.Type };

    public AnomalyAlert? Detect(DetectionContext context)
    {
        var message = context.Message;
        var previous = context.Previous;

        if (previous == null)
            return null;

        double jumpKm = Distance(previous, message);

        // The latest report must be far from this one...
        if (jumpKm < _minimumDistanceKm)
            return null;

        // ...while an earlier report inside the window was right next to it.
        var returnPoint = context.History
            .Where(h => h.TimestampUtc != previous.TimestampUtc)
            .Where(h => Math.Abs((message.TimestampUtc - h.TimestampUtc).TotalSeconds) <= _windowSeconds)
            .FirstOrDefault(h => Distance(h, message) < _minimumDistanceKm / 2);

        if (returnPoint == null)
            return null;

        return new AnomalyAlert
        {
            Icao = message.Icao,
            AnomalyType = Type,
            RiskScore = 95,
            Description =
                $"{message.Icao} is being reported from two locations {jumpKm:F0} km apart " +
                $"within {_windowSeconds:F0} s. Possible identity duplication.",
            DetectedAtUtc = DateTimeOffset.UtcNow,
            Latitude = message.Latitude,
            Longitude = message.Longitude
        };
    }

    private static double Distance(AircraftMessage a, AircraftMessage b) =>
        HaversineCalculator.CalculateDistance(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    private static double ReadPositive(string? value, double fallback) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) &&
        double.IsFinite(v) && v > 0 ? v : fallback;
}
