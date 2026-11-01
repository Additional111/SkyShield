using Microsoft.AspNetCore.SignalR;
using SkyShield.Hubs;
using SkyShield.Models;
using SkyShield.Services.AircraftState;
using SkyShield.Services.Detection;
using SkyShield.Services.RiskScoring;

namespace SkyShield.Services.Processing;

/// <summary>
/// The one pipeline every message goes through, whether it arrives on the REST
/// ingestion endpoint or on the simulation endpoint:
/// detection -> risk scoring -> SignalR broadcast.
/// The caller chooses which state store the message is evaluated against.
/// </summary>
public class TelemetryProcessor
{
    private readonly IAnomalyDetectionService _detection;
    private readonly IRiskScoringService _risk;
    private readonly IHubContext<SkyShieldHub> _hub;

    public TelemetryProcessor(
        IAnomalyDetectionService detection,
        IRiskScoringService risk,
        IHubContext<SkyShieldHub> hub)
    {
        _detection = detection;
        _risk = risk;
        _hub = hub;
    }

    public async Task<ProcessingResult> ProcessAsync(
        AircraftMessage message,
        IAircraftStateService state,
        bool broadcastAircraft,
        CancellationToken cancellationToken = default)
    {
        message.Icao = message.Icao.Trim().ToUpperInvariant();

        var alerts = _detection.Analyze(message, state);

        foreach (var alert in alerts)
            alert.Severity = _risk.GetRiskLevel(alert.RiskScore);

        var risk = _risk.CalculateRiskScore(message.Icao, alerts.ToList());

        if (broadcastAircraft)
            await _hub.Clients.All.SendAsync("AircraftUpdated", message, cancellationToken);

        foreach (var alert in alerts)
            await _hub.Clients.All.SendAsync("AlertRaised", alert, cancellationToken);

        return new ProcessingResult(message, alerts, risk);
    }
}
