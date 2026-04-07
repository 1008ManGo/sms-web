using Microsoft.AspNetCore.Mvc;
using SmppGateway.Services;

namespace SmppGateway.Controllers;

[ApiController]
[Route("api/v1")]
public class HealthController : ControllerBase
{
    private readonly ISmppClientManager _smppClientManager;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ISmppClientManager smppClientManager,
        ILogger<HealthController> logger)
    {
        _smppClientManager = smppClientManager;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        var healthySessions = _smppClientManager.HealthySessions;
        var isHealthy = healthySessions > 0;

        var response = new
        {
            Status = isHealthy ? "healthy" : "unhealthy",
            Timestamp = DateTime.UtcNow,
            Sessions = new
            {
                Total = _smppClientManager.TotalSessions,
                Healthy = healthySessions
            }
        };

        if (isHealthy)
        {
            return Ok(response);
        }

        return StatusCode(503, response);
    }

    [HttpGet("channels/status")]
    public IActionResult GetChannelStatus()
    {
        var routeStrategy = _smppClientManager.GetRouteStrategy();
        var channels = routeStrategy.GetAllPools().Select(pool => new
        {
            AccountId = pool.AccountId,
            TotalSessions = pool.TotalSessionCount,
            HealthySessions = pool.SessionCount,
            Status = pool.SessionCount > 0 ? "healthy" : "unhealthy"
        });

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            Channels = channels
        });
    }
}
