using SkyShield.Models;
using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Detection;

/// <summary>Flags telemetry from an ICAO that is not part of the known fleet.</summary>
public class GhostAircraftRule : IDetectionRule
{
    public const string Type = "Ghost Aircraft";

    private readonly FleetRegistry _fleet;

    public GhostAircraftRule(FleetRegistry fleet) => _fleet = fleet;

    public AnomalyAlert? Detect(DetectionContext context)
    {
        var message = context.Message;

        if (_fleet.IsKnown(message.Icao))
            return null;

        return new AnomalyAlert
        {
            Icao = message.Icao,
            AnomalyType = Type,
            RiskScore = 55,
            Description = $"Aircraft {message.Icao} is transmitting but is not in the authorized fleet.",
            DetectedAtUtc = DateTimeOffset.UtcNow,
            Latitude = message.Latitude,
            Longitude = message.Longitude
        };
    }
}
