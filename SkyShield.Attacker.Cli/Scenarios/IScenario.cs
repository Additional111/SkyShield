using SkyShield.Attacker.Cli.Models;

namespace SkyShield.Attacker.Cli.Scenarios;

public interface IScenario
{
    string Name { get; }

    string Description { get; }

    /// <summary>Alternative names accepted on the command line.</summary>
    string[] Aliases { get; }

    /// <summary>
    /// Builds the synthetic telemetry sequence. <paramref name="baseline"/> is the
    /// current position of a real fleet aircraft (copy only; nothing is modified).
    /// </summary>
    IReadOnlyList<TelemetryDto> Build(TelemetryDto baseline);

    /// <summary>True when the scenario needs a known fleet aircraft as its baseline.</summary>
    bool UsesFleetAircraft { get; }
}
