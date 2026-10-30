using SkyShield.Models;

namespace SkyShield.Services.Detection;

public interface IAnomalyDetectionService
{
    Task<IReadOnlyList<AnomalyAlert>> AnalyzeAsync(
        AircraftMessage message,
        CancellationToken cancellationToken = default);
}