namespace SkyShield.Models;

/// <summary>Outcome of running one telemetry message through the full pipeline.</summary>
public sealed record ProcessingResult(
    AircraftMessage Message,
    IReadOnlyList<AnomalyAlert> Alerts,
    SecurityRiskScore Risk);

/// <summary>Body of POST /api/simulation/events.</summary>
public class SimulationEventRequest
{
    public string Scenario { get; set; } = string.Empty;

    public AircraftMessage? Message { get; set; }
}

/// <summary>Payload of the "SystemStatus" SignalR event.</summary>
public sealed record SystemStatusDto(
    bool MonitoringActive,
    int AircraftCount,
    int SimulatedAircraftCount,
    DateTimeOffset ServerTimeUtc);
