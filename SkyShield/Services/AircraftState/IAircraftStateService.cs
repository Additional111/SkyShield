using SkyShield.Models;

namespace SkyShield.Services.AircraftState;

/// <summary>Result of storing a message: what was known before it.</summary>
public sealed record StateUpdate(
    AircraftMessage? Previous,
    IReadOnlyList<AircraftMessage> History);

public interface IAircraftStateService
{
    AircraftMessage? GetState(string icao);

    IReadOnlyList<AircraftMessage> GetAllStates();

    /// <summary>
    /// Stores the message as the newest known state (older messages are ignored)
    /// and returns the previous state plus recent history, oldest first.
    /// </summary>
    StateUpdate Update(AircraftMessage message);

    void Clear();
}
