using Microsoft.AspNetCore.Mvc;

namespace FocusTrack.Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    //indicates the service is running
    [HttpGet("healthz")]
    public IActionResult Healthz()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    //indicates the service is ready to accept traffic
    [HttpGet("readyz")]
    public IActionResult Readyz()
    {
        // TODO: Add checks for dependencies (database, message broker, etc)
        return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
    }
}
