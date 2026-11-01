using Microsoft.AspNetCore.Mvc;
using SkyShield.Models;
using SkyShield.Services.AircraftState;
using SkyShield.Services.Ingestion;
using SkyShield.Services.Processing;

namespace SkyShield.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly AircraftValidationService _validation;
    private readonly TelemetryProcessor _processor;
    private readonly IAircraftStateService _state;

    public FlightsController(
        AircraftValidationService validation,
        TelemetryProcessor processor,
        IAircraftStateService state)
    {
        _validation = validation;
        _processor = processor;
        _state = state;
    }

    /// <summary>All currently tracked aircraft.</summary>
    [HttpGet]
    public IActionResult GetFlights() => Ok(_state.GetAllStates());

    /// <summary>One tracked aircraft.</summary>
    [HttpGet("{icao}")]
    public IActionResult GetFlight(string icao)
    {
        var aircraft = _state.GetState(icao);
        return aircraft == null ? NotFound(new { success = false, error = "Aircraft not tracked." }) : Ok(aircraft);
    }

    /// <summary>Ingests one telemetry message from normal traffic.</summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveFlight(
        [FromBody] AircraftMessage message,
        CancellationToken cancellationToken)
    {
        if (!_validation.IsValid(message, out string error))
            return BadRequest(new { success = false, error });

        var result = await _processor.ProcessAsync(message, _state, true, cancellationToken);

        return Ok(new
        {
            success = true,
            icao = result.Message.Icao,
            anomalyDetected = result.Alerts.Count > 0,
            alerts = result.Alerts,
            riskScore = result.Risk
        });
    }
}
