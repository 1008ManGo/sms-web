namespace SmppClient.Resilience;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreaker
{
    private readonly string _name;
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly ILogger<CircuitBreaker> _logger;

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _openedAt = DateTime.MinValue;
    private readonly object _lock = new();

    public CircuitState State
    {
        get { lock (_lock) return _state; }
    }

    public CircuitBreaker(
        string name,
        int failureThreshold,
        TimeSpan openDuration,
        ILogger<CircuitBreaker> logger)
    {
        _name = name;
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
        _logger = logger;
    }

    public bool IsOpen => _state == CircuitState.Open;
    public bool IsClosed => _state == CircuitState.Closed;
    public bool IsHalfOpen => _state == CircuitState.HalfOpen;

    public async Task<bool> ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (!await CanExecuteAsync())
        {
            throw new CircuitBreakerOpenException($"Circuit breaker {_name} is open");
        }

        try
        {
            await action();
            OnSuccess();
            return true;
        }
        catch
        {
            OnFailure();
            throw;
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (!await CanExecuteAsync())
        {
            throw new CircuitBreakerOpenException($"Circuit breaker {_name} is open");
        }

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch
        {
            OnFailure();
            throw;
        }
    }

    private async Task<bool> CanExecuteAsync()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    if (DateTime.UtcNow - _openedAt > _openDuration)
                    {
                        _state = CircuitState.HalfOpen;
                        _logger.LogInformation("Circuit breaker {Name} transitioned to HalfOpen", _name);
                        return true;
                    }
                    return false;

                case CircuitState.HalfOpen:
                    return true;

                default:
                    return false;
            }
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Closed;
                _failureCount = 0;
                _logger.LogInformation("Circuit breaker {Name} transitioned to Closed", _name);
            }
            else if (_state == CircuitState.Closed)
            {
                _failureCount = 0;
            }
        }
    }

    private void OnFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker {Name} transitioned to Open (half-open failure)", _name);
            }
            else if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker {Name} transitioned to Open after {Failures} failures",
                    _name, _failureCount);
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _failureCount = 0;
        }
    }

    public (CircuitState State, int FailureCount, DateTime? OpenedAt) GetStatus()
    {
        lock (_lock)
        {
            return (_state, _failureCount, _state == CircuitState.Open ? _openedAt : null);
        }
    }
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}

public class CircuitBreakerRegistry
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _breakers = new();
    private readonly ILogger<CircuitBreakerRegistry> _logger;

    public CircuitBreakerRegistry(ILogger<CircuitBreakerRegistry> logger)
    {
        _logger = logger;
    }

    public CircuitBreaker GetOrCreate(string name, int failureThreshold = 5, TimeSpan? openDuration = null)
    {
        return _breakers.GetOrAdd(name, _ =>
        {
            var logger = LoggerFactory.Create(b => b.AddConsole())
                .CreateLogger<CircuitBreaker>();
            return new CircuitBreaker(name, failureThreshold, openDuration ?? TimeSpan.FromSeconds(30), logger);
        });
    }

    public IEnumerable<(string Name, CircuitState State)> GetAllStatus()
    {
        return _breakers.Select(kvp => (kvp.Key, kvp.Value.State));
    }
}
