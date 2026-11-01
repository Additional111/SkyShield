using SkyShield.Models;

namespace SkyShield.Services.Detection;

/// <summary>Everything a rule may look at when judging one message.</summary>
public sealed record DetectionContext(
    AircraftMessage Message,
    AircraftMessage? Previous,
    IReadOnlyList<AircraftMessage> History);

public interface IDetectionRule
{
    AnomalyAlert? Detect(DetectionContext context);

    /// <summary>
    /// Anomaly types that become redundant when this rule fires
    /// (e.g. identity duplication already explains an impossible movement).
    /// </summary>
    IReadOnlyCollection<string> Supersedes => Array.Empty<string>();
}
