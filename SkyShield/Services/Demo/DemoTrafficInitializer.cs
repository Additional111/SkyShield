using SkyShield.Services.AircraftState;

namespace SkyShield.Services.Demo;

/// <summary>Seeds the state store with the synthetic fleet and registers it as authorized.</summary>
public class DemoTrafficInitializer
{
    private readonly DemoAircraftGenerator _generator;
    private readonly IAircraftStateService _state;
    private readonly FleetRegistry _fleet;
    private readonly ILogger<DemoTrafficInitializer> _logger;

    public DemoTrafficInitializer(
        DemoAircraftGenerator generator,
        IAircraftStateService state,
        FleetRegistry fleet,
        ILogger<DemoTrafficInitializer> logger)
    {
        _generator = generator;
        _state = state;
        _fleet = fleet;
        _logger = logger;
    }

    public void Initialize()
    {
        var aircraft = _generator.GenerateAircraft();

        foreach (var message in aircraft)
        {
            _fleet.Register(message.Icao);
            _state.Update(message);
        }

        _logger.LogInformation("Initialized {Count} synthetic aircraft.", aircraft.Count);
    }
}
