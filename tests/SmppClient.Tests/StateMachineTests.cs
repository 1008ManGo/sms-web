using Microsoft.Extensions.Logging;
using SmppClient.Connection;
using Xunit;

namespace SmppClient.Tests;

public class StateMachineTests
{
    [Fact]
    public void InitialState_IsClosed()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StateMachine>();
        var sm = new StateMachine(logger);
        Assert.Equal(SessionState.Closed, sm.CurrentState);
        Assert.True(sm.IsClosed);
        Assert.False(sm.CanSend);
    }

    [Fact]
    public void Connect_TransitionsToConnecting()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StateMachine>();
        var sm = new StateMachine(logger);
        
        Assert.True(sm.TryTransition(SessionStateEvent.Connect));
        Assert.Equal(SessionState.Connecting, sm.CurrentState);
    }

    [Fact]
    public void BindSuccess_FromConnecting_TransitionsToBound()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StateMachine>();
        var sm = new StateMachine(logger);
        
        sm.TryTransition(SessionStateEvent.Connect);
        sm.TryTransition(SessionStateEvent.ConnectSuccess);
        Assert.Equal(SessionState.Binding, sm.CurrentState);
        
        sm.TryTransition(SessionStateEvent.BindSuccess);
        Assert.Equal(SessionState.Bound, sm.CurrentState);
        Assert.True(sm.CanSend);
    }

    [Fact]
    public void Unbind_FromBound_TransitionsToUnbinding()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StateMachine>();
        var sm = new StateMachine(logger);
        
        sm.TryTransition(SessionStateEvent.Connect);
        sm.TryTransition(SessionStateEvent.ConnectSuccess);
        sm.TryTransition(SessionStateEvent.BindSuccess);
        
        Assert.True(sm.TryTransition(SessionStateEvent.Unbind));
        Assert.Equal(SessionState.Unbinding, sm.CurrentState);
        Assert.False(sm.CanSend);
    }

    [Fact]
    public void UnbindComplete_TransitionsToClosed()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StateMachine>();
        var sm = new StateMachine(logger);
        
        sm.TryTransition(SessionStateEvent.Connect);
        sm.TryTransition(SessionStateEvent.ConnectSuccess);
        sm.TryTransition(SessionStateEvent.BindSuccess);
        sm.TryTransition(SessionStateEvent.Unbind);
        
        Assert.True(sm.TryTransition(SessionStateEvent.UnbindComplete));
        Assert.Equal(SessionState.Closed, sm.CurrentState);
        Assert.True(sm.IsClosed);
    }

    [Fact]
    public void ConnectionLost_FromBound_TransitionsToClosed()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StateMachine>();
        var sm = new StateMachine(logger);
        
        sm.TryTransition(SessionStateEvent.Connect);
        sm.TryTransition(SessionStateEvent.ConnectSuccess);
        sm.TryTransition(SessionStateEvent.BindSuccess);
        
        Assert.True(sm.TryTransition(SessionStateEvent.ConnectionLost));
        Assert.Equal(SessionState.Closed, sm.CurrentState);
    }

    [Fact]
    public void InvalidTransition_ReturnsFalse()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StateMachine>();
        var sm = new StateMachine(logger);
        
        Assert.False(sm.TryTransition(SessionStateEvent.Unbind));
        Assert.False(sm.TryTransition(SessionStateEvent.BindSuccess));
    }
}
