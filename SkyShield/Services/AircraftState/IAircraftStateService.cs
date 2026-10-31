using SkyShield.Models;

namespace SkyShield.Services.AircraftState;

public interface IAircraftStateService
{
    AircraftMessage? GetState(string icao);

    AircraftMessage? UpdateState(AircraftMessage message);

    IReadOnlyList<AircraftMessage> GetAllStates();
}