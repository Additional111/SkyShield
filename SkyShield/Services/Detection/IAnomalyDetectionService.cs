using SkyShield.Models;
using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Detection;

public interface IAnomalyDetectionService
{
    /// <summary>Analyzes a message against the given state store (and updates it).</summary>
    IReadOnlyList<AnomalyAlert> Analyze(AircraftMessage message, IAircraftStateService state);
}
