namespace SkyShield.Models;

public class AnomalyAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Icao { get; set; } = string.Empty;

    public string AnomalyType { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public int RiskScore { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset DetectedAtUtc { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}