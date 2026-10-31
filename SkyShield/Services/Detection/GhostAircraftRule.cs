using Microsoft.Extensions.Configuration;
using SkyShield.Models;

namespace SkyShield.Services.Detection;

public class GhostAircraftRule : IDetectionRule
{
    private readonly HashSet<string> _authorizedAircraft;

    public GhostAircraftRule(IConfiguration configuration)
    {
        _authorizedAircraft = configuration
            .GetSection("AuthorizedAircraft")
            .GetChildren()
            .Select(x => x.Value?.Trim().ToUpperInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet()!;
    }

    public AnomalyAlert? Detect(
        AircraftMessage message,
        AircraftMessage? previousState)
    {
        string icao = message.Icao?.Trim().ToUpperInvariant() ?? "";

        if (_authorizedAircraft.Contains(icao))
        {
            return null;
        }

        return new AnomalyAlert
        {
            Icao = icao,
            AnomalyType = "Unknown Aircraft",
            Severity = "Medium",
            RiskScore = 30,
            Description = $"Aircraft with ICAO {icao} is not in the authorized aircraft list.",
            DetectedAtUtc = DateTimeOffset.UtcNow,
            Latitude = message.Latitude,
            Longitude = message.Longitude
        };
    }
}