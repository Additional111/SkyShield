using Microsoft.AspNetCore.Mvc;
using SkyShield.Models;
using SkyShield.Services.Ingestion;
using SkyShield.Services.Detection;

namespace SkyShield.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly AircraftValidationService _validationService;
    private readonly IAnomalyDetectionService _detectionService;

    public FlightsController(
        AircraftValidationService validationService,
        IAnomalyDetectionService detectionService)
    {
        _validationService = validationService;
        _detectionService = detectionService;
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

        return Ok(new
        {
            Success = true,
            Icao = message.Icao,
            Alerts = alerts
        });
    }
}