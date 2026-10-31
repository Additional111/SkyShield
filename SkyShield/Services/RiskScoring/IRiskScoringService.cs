using SkyShield.Models;

namespace SkyShield.Services.RiskScoring;

public interface IRiskScoringService
{
    SecurityRiskScore CalculateRiskScore(
        string icao,
        IReadOnlyCollection<AnomalyAlert> anomalies);
}