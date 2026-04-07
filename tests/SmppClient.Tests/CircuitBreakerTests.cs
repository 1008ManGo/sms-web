using SmppClient.Resilience;
using Xunit;

namespace SmppClient.Tests;

public class CircuitBreakerTests
{
    [Fact]
    public void InitialState_IsClosed()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CircuitBreaker>();
        var cb = new CircuitBreaker("test", 3, TimeSpan.FromSeconds(10), logger);

        Assert.True(cb.IsClosed);
        Assert.False(cb.IsOpen);
        Assert.False(cb.IsHalfOpen);
    }

    [Fact]
    public async Task ExecuteAsync_Success_DoesNotOpen()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CircuitBreaker>();
        var cb = new CircuitBreaker("test", 3, TimeSpan.FromSeconds(10), logger);

        for (int i = 0; i < 3; i++)
        {
            await cb.ExecuteAsync(() => Task.CompletedTask);
        }

        Assert.True(cb.IsClosed);
    }

    [Fact]
    public async Task ExecuteAsync_FailureCountReachesThreshold_Opens()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CircuitBreaker>();
        var cb = new CircuitBreaker("test", 3, TimeSpan.FromSeconds(10), logger);

        for (int i = 0; i < 3; i++)
        {
            try
            {
                await cb.ExecuteAsync<int>(() => throw new Exception("fail"));
            }
            catch { }
        }

        Assert.True(cb.IsOpen);
    }

    [Fact]
    public async Task ExecuteAsync_CircuitOpen_ThrowsException()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CircuitBreaker>();
        var cb = new CircuitBreaker("test", 1, TimeSpan.FromSeconds(10), logger);

        try
        {
            await cb.ExecuteAsync(() => throw new Exception("fail"));
        }
        catch { }

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            cb.ExecuteAsync(() => Task.CompletedTask));
    }
}

public class RateLimiterTests
{
    [Fact]
    public void InitialState_AllowsRequest()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<RateLimiter>();
        var limiter = new RateLimiter(10, logger);

        Assert.True(limiter.TryAcquire());
    }

    [Fact]
    public void ExceedLimit_ReturnsFalse()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<RateLimiter>();
        var limiter = new RateLimiter(2, logger);

        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }
}
