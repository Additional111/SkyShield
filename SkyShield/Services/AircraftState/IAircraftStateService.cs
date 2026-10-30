namespace SkyShield.Services.AircraftState;

public interface IAircraftStateService
{
    AircraftStateEntry? GetState(string icao);

    AircraftStateEntry? UpdateState(AircraftStateEntry state);
}