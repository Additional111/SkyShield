namespace SkyShield.Models;

public class SecurityRiskScore
{
    public string Icao { get; set; } = string.Empty;

    public int Score { get; set; }

    public string RiskLevel { get; set; } = string.Empty;

    public DateTimeOffset CalculatedAtUtc { get; set; }

    public List<string> Reasons { get; set; } = new();
}