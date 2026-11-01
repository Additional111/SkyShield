using SkyShield.Models;

namespace SkyShield.Services.RiskScoring;

public interface IRiskScoringService
{
    SecurityRiskScore CalculateRiskScore(string icao, IReadOnlyCollection<AnomalyAlert> anomalies);

    /// <summary>Maps a 0-100 score to Low / Medium / High / Critical.</summary>
    string GetRiskLevel(int score);
}
