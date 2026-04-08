using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmppGateway.Observability;
using SmppGateway.Services;

namespace SmppGateway.Controllers;

[ApiController]
[Route("api/v1")]
public class HealthController : ControllerBase
{
    private readonly Services.ISmppClientManager _smppClientManager;
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        Services.ISmppClientManager smppClientManager,
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _smppClientManager = smppClientManager;
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        var healthySessions = _smppClientManager.HealthySessions;
        var isHealthy = report.Status == HealthStatus.Healthy && healthySessions > 0;

        var response = new
        {
            Status = isHealthy ? "healthy" : "unhealthy",
            Timestamp = DateTime.UtcNow,
            Sessions = new
            {
                Total = _smppClientManager.TotalSessions,
                Healthy = healthySessions
            },
            Components = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration.TotalMilliseconds
            })
        };

        if (isHealthy)
        {
            return Ok(response);
        }

        return StatusCode(503, response);
    }

    [HttpGet("health/live")]
    public IActionResult Live()
    {
        return Ok(new { Status = "alive", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("health/ready")]
    public async Task<IActionResult> Ready()
    {
        var healthySessions = _smppClientManager.HealthySessions;
        if (healthySessions > 0)
        {
            return Ok(new { Status = "ready", Timestamp = DateTime.UtcNow });
        }
        return StatusCode(503, new { Status = "not_ready", Timestamp = DateTime.UtcNow });
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
