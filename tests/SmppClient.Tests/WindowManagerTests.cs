using Microsoft.Extensions.Logging;
using SmppClient.Core;
using Xunit;

namespace SmppClient.Tests;

public class WindowManagerTests
{
    [Fact]
    public void InitialState_Empty()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WindowManager>();
        var wm = new WindowManager(10, logger);
        
        Assert.Equal(0, wm.PendingCount);
        Assert.False(wm.IsFull);
        Assert.Equal(0, wm.UsagePercentage);
    }

    [Fact]
    public void Acquire_IncreasesPendingCount()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WindowManager>();
        var wm = new WindowManager(10, logger);
        
        Assert.True(wm.TryAcquire());
        Assert.Equal(1, wm.PendingCount);
        Assert.False(wm.IsFull);
    }

    [Fact]
    public void Acquire_WhenFull_ReturnsFalse()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WindowManager>();
        var wm = new WindowManager(2, logger);
        
        Assert.True(wm.TryAcquire());
        Assert.True(wm.TryAcquire());
        Assert.False(wm.TryAcquire());
        Assert.True(wm.IsFull);
    }

    [Fact]
    public void Release_DecreasesPendingCount()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WindowManager>();
        var wm = new WindowManager(10, logger);
        
        wm.TryAcquire();
        wm.TryAcquire();
        wm.Release();
        
        Assert.Equal(1, wm.PendingCount);
        Assert.False(wm.IsFull);
    }

    [Fact]
    public void Release_WhenEmpty_DoesNotGoNegative()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WindowManager>();
        var wm = new WindowManager(10, logger);
        
        wm.Release();
        Assert.Equal(0, wm.PendingCount);
    }

    [Fact]
    public void UsagePercentage_CalculatesCorrectly()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WindowManager>();
        var wm = new WindowManager(10, logger);
        
        wm.TryAcquire();
        wm.TryAcquire();
        
        Assert.Equal(20.0, wm.UsagePercentage);
    }

    [Fact]
    public async Task AcquireAsync_WaitsWhenFull()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WindowManager>();
        var wm = new WindowManager(1, logger);
        
        Assert.True(await wm.AcquireAsync());
        var acquireTask = wm.AcquireAsync();
        Assert.False(acquireTask.IsCompleted);
        
        wm.Release();
        var result = await acquireTask.WaitAsync(TimeSpan.FromSeconds(1));
        
        Assert.True(result);
    }
}
