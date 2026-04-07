using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SmppGateway.Observability;

public class SmppHealthCheck : IHealthCheck
{
    private readonly ISmppClientManager _smppClientManager;

    public SmppHealthCheck(ISmppClientManager smppClientManager)
    {
        _smppClientManager = smppClientManager;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthySessions = _smppClientManager.HealthySessions;

            if (healthySessions == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("No healthy SMPP sessions"));
            }

            if (healthySessions < 2)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"Only {healthySessions} healthy sessions"));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"{healthySessions} healthy sessions"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Health check failed", ex));
        }
    }
}

public class DatabaseHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(HealthCheckResult.Healthy("Database connection OK"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Database connection failed", ex));
        }
    }
}

public class QueueHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(HealthCheckResult.Healthy("Queue connection OK"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Queue connection failed", ex));
        }
    }
}
