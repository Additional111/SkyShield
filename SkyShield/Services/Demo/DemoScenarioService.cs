using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Demo;

public class DemoScenarioService
{
    private readonly DemoAircraftGenerator _aircraftGenerator;
    private readonly IAircraftStateService _aircraftStateService;

    public DemoScenarioService(
        DemoAircraftGenerator aircraftGenerator,
        IAircraftStateService aircraftStateService)
    {
        _aircraftGenerator = aircraftGenerator;
        _aircraftStateService = aircraftStateService;
    }

    public void Initialize()
    {
        var aircraft = _aircraftGenerator.GenerateAircraft();

        foreach (var message in aircraft)
        {
            _aircraftStateService.UpdateState(message);
        }

        Console.WriteLine(
            $"[DEMO] Initialized {aircraft.Count} synthetic aircraft.");
    }
}