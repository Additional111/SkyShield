using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SkyShield.Models;
using SkyShield.Hubs;
using SkyShield.Services.Ingestion;
using SkyShield.Services.Detection;
using SkyShield.Services.RiskScoring;
using SkyShield.Services.AircraftState;

namespace SkyShield.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly AircraftValidationService _validationService;
    private readonly IAnomalyDetectionService _detectionService;
    private readonly IRiskScoringService _riskScoringService;
    private readonly IAircraftStateService _aircraftStateService;
    private readonly IHubContext<SkyShieldHub> _hubContext;

    public FlightsController(
        AircraftValidationService validationService,
        IAnomalyDetectionService detectionService,
        IRiskScoringService riskScoringService,
        IAircraftStateService aircraftStateService,
        IHubContext<SkyShieldHub> hubContext)
    {
        _validationService = validationService;
        _detectionService = detectionService;
        _riskScoringService = riskScoringService;
        _aircraftStateService = aircraftStateService;
        _hubContext = hubContext;
    }

    [HttpGet]
    public IActionResult GetFlights()
    {
        var aircraft = _aircraftStateService.GetAllStates();

        return Ok(aircraft);
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveFlight(
        [FromBody] AircraftMessage message,
        CancellationToken cancellationToken)
    {
        if (!_validationService.IsValid(message, out string error))
        {
            return BadRequest(new
            {
                Success = false,
                Error = error
            });
        }

        var alerts = await _detectionService.AnalyzeAsync(
            message,
            cancellationToken);

        var riskScore = _riskScoringService.CalculateRiskScore(
            message.Icao,
            alerts);

        await _hubContext.Clients.All.SendAsync(
            "AircraftUpdated",
            message,
            cancellationToken);

        foreach (var alert in alerts)
        {
            await _hubContext.Clients.All.SendAsync(
                "AlertRaised",
                alert,
                cancellationToken);
        }

        return Ok(new
        {
            Success = true,
            Icao = message.Icao,
            Alerts = alerts,
            RiskScore = riskScore
        });
    }
}