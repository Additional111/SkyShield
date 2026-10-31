using SkyShield.Models;

namespace SkyShield.Services.RiskScoring;

public class RiskScoringService : IRiskScoringService
{
    public SecurityRiskScore CalculateRiskScore(
        string icao,
        IReadOnlyCollection<AnomalyAlert> anomalies)
    {
        int totalScore = 0;

        List<string> reasons = new();

        foreach (var anomaly in anomalies)
        {
            totalScore += anomaly.RiskScore;
            reasons.Add(anomaly.Description);
        }

        totalScore = Math.Clamp(totalScore, 0, 100);

        string riskLevel = GetRiskLevel(totalScore);

        return new SecurityRiskScore
        {
            Icao = icao,
            Score = totalScore,
            RiskLevel = riskLevel,
            CalculatedAtUtc = DateTimeOffset.UtcNow,
            Reasons = reasons
        };
    }

    private static string GetRiskLevel(int score)
    {
        if (score >= 70)
            return "Critical";

        if (score >= 30)
            return "Medium";

        return "Low";
    }
}