using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Mvc;

namespace HomeDB.Controllers;

[ApiController]
[Route("healthCheck")]
public class HealthController : ControllerBase
{
    private readonly Logger _logger;

    public HealthController(Logger logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        await using OperationLogScope scope = _logger.BeginScope(
            source: nameof(HealthController),
            operation: nameof(Get));

        return Ok(new { status = "ok" });
    }
}
