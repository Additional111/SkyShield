using SkyShield.Models;

namespace SkyShield.Services.Detection;

public interface IDetectionRule
{
    AnomalyAlert? Detect(
        AircraftMessage message,
        AircraftMessage? previousState);
}