using System.Collections.Concurrent;

namespace SmppClient.Resilience;

public class RateLimiter
{
    private readonly int _maxTps;
    private readonly object _lock = new();
    private readonly Queue<DateTime> _timestamps = new();
    private readonly ILogger<RateLimiter> _logger;

    public RateLimiter(int maxTps, ILogger<RateLimiter> logger)
    {
        _maxTps = maxTps;
        _logger = logger;
    }

    public async Task<bool> AcquireAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            while (_timestamps.Count > 0 && (now - _timestamps.Peek()).TotalSeconds >= 1)
            {
                _timestamps.Dequeue();
            }

            if (_timestamps.Count < _maxTps)
            {
                _timestamps.Enqueue(now);
                return true;
            }

            return false;
        }
    }

    public bool TryAcquire()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            while (_timestamps.Count > 0 && (now - _timestamps.Peek()).TotalSeconds >= 1)
            {
                _timestamps.Dequeue();
            }

            if (_timestamps.Count < _maxTps)
            {
                _timestamps.Enqueue(now);
                return true;
            }

            return false;
        }
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (TryAcquire())
                return;

            await Task.Delay(10, cancellationToken);
        }
    }

    public double CurrentTps
    {
        get
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                while (_timestamps.Count > 0 && (now - _timestamps.Peek()).TotalSeconds >= 1)
                {
                    _timestamps.Dequeue();
                }
                return _timestamps.Count;
            }
        }
    }

    public int MaxTps => _maxTps;
}

public class AccountRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();
    private readonly ILogger<AccountRateLimiter> _logger;

    public AccountRateLimiter(ILogger<AccountRateLimiter> logger)
    {
        _logger = logger;
    }

    public RateLimiter GetOrCreate(string accountId, int maxTps)
    {
        return _limiters.GetOrAdd(accountId, _ =>
        {
            var logger = LoggerFactory.Create(b => b.AddConsole())
                .CreateLogger<RateLimiter>();
            return new RateLimiter(maxTps, logger);
        });
    }

    public async Task<bool> AcquireAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (_limiters.TryGetValue(accountId, out var limiter))
        {
            return await limiter.AcquireAsync(cancellationToken);
        }
        return true;
    }

    public IEnumerable<(string AccountId, double CurrentTps, int MaxTps)> GetAllStats()
    {
        return _limiters.Select(kvp => (kvp.Key, kvp.Value.CurrentTps, kvp.Value.MaxTps));
    }
}
