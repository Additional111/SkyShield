using SkyShield.Models;
using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Detection;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IAircraftStateService _aircraftStateService;
    private readonly IEnumerable<IDetectionRule> _rules;

    public AnomalyDetectionService(
        IAircraftStateService aircraftStateService,
        IEnumerable<IDetectionRule> rules)
    {
        _aircraftStateService = aircraftStateService;
        _rules = rules;
    }

    public Task<IReadOnlyList<AnomalyAlert>> AnalyzeAsync(
        AircraftMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var alerts = new List<AnomalyAlert>();

        AircraftMessage? previousState = null;

        if (!string.IsNullOrWhiteSpace(message.Icao))
        {
            if (message.TimestampUtc != default)
            {
                previousState = _aircraftStateService.UpdateState(message);
            }
            else
            {
                previousState = _aircraftStateService.GetState(message.Icao);
            }
        }

        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var alert = rule.Detect(message, previousState);

            if (alert != null)
            {
                alerts.Add(alert);
            }
        }

        return Task.FromResult<IReadOnlyList<AnomalyAlert>>(
            alerts.AsReadOnly());
    }
}