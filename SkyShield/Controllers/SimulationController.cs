using Microsoft.AspNetCore.Mvc;
using SkyShield.Models;
using SkyShield.Services.Ingestion;
using SkyShield.Services.Simulation;

namespace SkyShield.Controllers;

/// <summary>
/// Local-only interface used by SkyShield.Attacker.Cli to submit synthetic test
/// events. Events are validated and processed by the real detection and risk
/// scoring components; the backend (not the CLI) raises the alerts.
/// </summary>
[ApiController]
[Route("api/simulation")]
public class git status
    git branchSimulationController : ControllerBase
{
    private readonly SimulationService _simulation;
    private readonly AircraftValidationService _validation;

    public SimulationController(SimulationService simulation, AircraftValidationService validation)
    {
        _simulation = simulation;
        _validation = validation;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        if (!IsAllowed(out var denied)) return denied!;

        return Ok(new { enabled = _simulation.Enabled, temporaryAircraft = _simulation.GhostCount });
    }

    [HttpPost("events")]
    public async Task<IActionResult> SubmitEvent(
        [FromBody] SimulationEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAllowed(out var denied)) return denied!;

        if (string.IsNullOrWhiteSpace(request.Scenario))
            return BadRequest(new { success = false, error = "Scenario name is required." });

        if (!_validation.IsValid(request.Message, out string error))
            return BadRequest(new { success = false, error });

        var result = await _simulation.SubmitAsync(request.Message!, cancellationToken);

        return Ok(new
        {
            success = true,
            scenario = request.Scenario,
            icao = result.Message.Icao,
            anomalyDetected = result.Alerts.Count > 0,
            alerts = result.Alerts,
            riskScore = result.Risk
        });
    }

    /// <summary>Clears temporary simulation state. broadcast=true also clears dashboards.</summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromQuery] bool broadcast = true, CancellationToken cancellationToken = default)
    {
        if (!IsAllowed(out var denied)) return denied!;

        await _simulation.ResetAsync(broadcast, cancellationToken);
        return Ok(new { success = true, broadcast });
    }

    private bool IsAllowed(out IActionResult? denied)
    {
        denied = null;

        if (!_simulation.Enabled)
        {
            denied = NotFound(new { success = false, error = "Simulation interface is disabled." });
            return false;
        }

        var remote = HttpContext.Connection.RemoteIpAddress;
        if (remote == null || !System.Net.IPAddress.IsLoopback(remote))
        {
            denied = StatusCode(StatusCodes.Status403Forbidden,
                new { success = false, error = "Simulation interface accepts local requests only." });
            return false;
        }

        return true;
    }
}
