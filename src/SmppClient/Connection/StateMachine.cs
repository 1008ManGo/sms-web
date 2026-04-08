using Microsoft.Extensions.Logging;

namespace SmppClient.Connection;

public enum SessionState
{
    Closed,
    Connecting,
    Binding,
    Bound,
    Unbinding
}

public enum SessionStateEvent
{
    Connect,
    ConnectSuccess,
    ConnectFailed,
    BindSuccess,
    BindFailed,
    Unbind,
    UnbindComplete,
    ConnectionLost,
    Timeout
}

public class StateMachine
{
    private SessionState _currentState = SessionState.Closed;
    private readonly object _lock = new();
    private readonly ILogger<StateMachine> _logger;

    public SessionState CurrentState
    {
        get { lock (_lock) return _currentState; }
    }

    public StateMachine(ILogger<StateMachine> logger)
    {
        _logger = logger;
    }

    public bool CanTransition(SessionStateEvent @event)
    {
        lock (_lock)
        {
            return (_currentState, @event) switch
            {
                (SessionState.Closed, SessionStateEvent.Connect) => true,
                (SessionState.Closed, SessionStateEvent.ConnectionLost) => false,
                (SessionState.Connecting, SessionStateEvent.ConnectSuccess) => true,
                (SessionState.Connecting, SessionStateEvent.ConnectFailed) => true,
                (SessionState.Connecting, SessionStateEvent.ConnectionLost) => true,
                (SessionState.Binding, SessionStateEvent.BindSuccess) => true,
                (SessionState.Binding, SessionStateEvent.BindFailed) => true,
                (SessionState.Binding, SessionStateEvent.ConnectionLost) => true,
                (SessionState.Bound, SessionStateEvent.Unbind) => true,
                (SessionState.Bound, SessionStateEvent.ConnectionLost) => true,
                (SessionState.Unbinding, SessionStateEvent.UnbindComplete) => true,
                (SessionState.Unbinding, SessionStateEvent.ConnectionLost) => true,
                _ => false
            };
        }
    }

    public bool TryTransition(SessionStateEvent @event)
    {
        lock (_lock)
        {
            if (!CanTransition(@event))
            {
                _logger.LogWarning("Invalid transition: {State} + {Event}", _currentState, @event);
                return false;
            }

            var previousState = _currentState;
            var newState = (_currentState, @event) switch
            {
                (SessionState.Closed, SessionStateEvent.Connect) => SessionState.Connecting,
                (SessionState.Connecting, SessionStateEvent.ConnectSuccess) => SessionState.Binding,
                (SessionState.Connecting, SessionStateEvent.ConnectFailed) => SessionState.Closed,
                (SessionState.Binding, SessionStateEvent.BindSuccess) => SessionState.Bound,
                (SessionState.Binding, SessionStateEvent.BindFailed) => SessionState.Closed,
                (SessionState.Bound, SessionStateEvent.Unbind) => SessionState.Unbinding,
                (SessionState.Unbinding, SessionStateEvent.UnbindComplete) => SessionState.Closed,
                (SessionState.Connecting, SessionStateEvent.ConnectionLost) => SessionState.Closed,
                (SessionState.Binding, SessionStateEvent.ConnectionLost) => SessionState.Closed,
                (SessionState.Bound, SessionStateEvent.ConnectionLost) => SessionState.Closed,
                (SessionState.Unbinding, SessionStateEvent.ConnectionLost) => SessionState.Closed,
                _ => _currentState
            };

            if (newState != _currentState)
            {
                _logger.LogInformation("State transition: {PrevState} -> {NewState} ({Event})",
                    previousState, newState, @event);
                _currentState = newState;
            }

            return true;
        }
    }

    public bool IsBound => _currentState == SessionState.Bound;
    public bool IsClosed => _currentState == SessionState.Closed;
    public bool CanSend => _currentState == SessionState.Bound;
}
