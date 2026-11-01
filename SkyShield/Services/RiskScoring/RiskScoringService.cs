using SkyShield.Models;

namespace SkyShield.Services.RiskScoring;

/// <summary>
/// Single place that defines how scores map to severity:
/// 0-29 Low, 30-59 Medium, 60-79 High, 80-100 Critical.
/// The total for an aircraft is the sum of its anomaly scores, capped at 100.
/// </summary>
public class RiskScoringService : IRiskScoringService
{
    public SecurityRiskScore CalculateRiskScore(string icao, IReadOnlyCollection<AnomalyAlert> anomalies)
    {
        int total = Math.Clamp(anomalies.Sum(a => a.RiskScore), 0, 100);

        return new SecurityRiskScore
        {
            Icao = icao,
            Score = total,
            RiskLevel = GetRiskLevel(total),
            CalculatedAtUtc = DateTimeOffset.UtcNow,
            Reasons = anomalies
                .Select(a => $"{a.AnomalyType} (+{a.RiskScore}): {a.Description}")
                .ToList()
        };
    }

    public string GetRiskLevel(int score) => score switch
    {
        >= 80 => "Critical",
        >= 60 => "High",
        >= 30 => "Medium",
        _ => "Low"
    };
}
