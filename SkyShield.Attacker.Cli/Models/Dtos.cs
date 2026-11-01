namespace SkyShield.Attacker.Cli.Models;

public class TelemetryDto
{
    public string Icao { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AltitudeFeet { get; set; }
    public double GroundSpeedKnots { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
}

public record SimulationEventRequest(string Scenario, TelemetryDto Message);

public class AlertDto
{
    public string Icao { get; set; } = "";
    public string AnomalyType { get; set; } = "";
    public string Severity { get; set; } = "";
    public int RiskScore { get; set; }
    public string Description { get; set; } = "";
}

public class RiskDto
{
    public int Score { get; set; }
    public string RiskLevel { get; set; } = "";
}

public class SimulationResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool AnomalyDetected { get; set; }
    public List<AlertDto> Alerts { get; set; } = new();
    public RiskDto? RiskScore { get; set; }
}

public class HealthDto
{
    public string Status { get; set; } = "";
    public bool MonitoringActive { get; set; }
    public int AircraftCount { get; set; }
}
