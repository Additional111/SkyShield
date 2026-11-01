using Microsoft.AspNetCore.Mvc;
using SkyShield.Services.Processing;

namespace SkyShield.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly MonitoringStatus _status;

    public HealthController(MonitoringStatus status) => _status = status;

    [HttpGet]
    public IActionResult Get()
    {
        var s = _status.Build();
        return Ok(new { status = "ok", monitoringActive = s.MonitoringActive, aircraftCount = s.AircraftCount });
    }
}
