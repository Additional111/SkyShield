using Microsoft.AspNetCore.Mvc;
using SkyShield.Models;
using SkyShield.Services.Ingestion;
using SkyShield.Services.Detection;
using SkyShield.Services.RiskScoring;

namespace SkyShield.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly AircraftValidationService _validationService;
    private readonly IAnomalyDetectionService _detectionService;
    private readonly IRiskScoringService _riskScoringService;

    public FlightsController(
        AircraftValidationService validationService,
        IAnomalyDetectionService detectionService,
        IRiskScoringService riskScoringService)
    {
        _validationService = validationService;
        _detectionService = detectionService;
        _riskScoringService = riskScoringService;
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

        return Ok(new
        {
            Success = true,
            Icao = message.Icao,
            Alerts = alerts,
            RiskScore = riskScore
        });
    }
}