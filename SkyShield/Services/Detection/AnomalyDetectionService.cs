using SkyShield.Models;
using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Detection;

/// <summary>
/// Runs every registered rule against a message. Rules are independent:
/// add or remove one by changing its registration in Program.cs.
/// </summary>
public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IEnumerable<IDetectionRule> _rules;
    private readonly ILogger<AnomalyDetectionService> _logger;

    public AnomalyDetectionService(
        IEnumerable<IDetectionRule> rules,
        ILogger<AnomalyDetectionService> logger)
    {
        _rules = rules;
        _logger = logger;
    }

    public IReadOnlyList<AnomalyAlert> Analyze(AircraftMessage message, IAircraftStateService state)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(state);

        var update = state.Update(message);
        var context = new DetectionContext(message, update.Previous, update.History);

        var fired = new List<(IDetectionRule Rule, AnomalyAlert Alert)>();

        foreach (var rule in _rules)
        {
            try
            {
                var alert = rule.Detect(context);
                if (alert != null)
                    fired.Add((rule, alert));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detection rule {Rule} failed.", rule.GetType().Name);
            }
        }

        var superseded = fired
            .SelectMany(f => f.Rule.Supersedes)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return fired
            .Select(f => f.Alert)
            .Where(a => !superseded.Contains(a.AnomalyType))
            .ToList();
    }
}
