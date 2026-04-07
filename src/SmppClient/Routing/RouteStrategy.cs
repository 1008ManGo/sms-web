using SmppClient.Routing;

namespace SmppClient.Routing;

public enum RouteStrategyType
{
    Weight,
    Priority,
    LeastLoaded,
    RoundRobin
}

public class SmppAccount
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 2775;
    public string SystemId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SystemType { get; set; } = "SMPP";
    public int Weight { get; set; } = 100;
    public int Priority { get; set; } = 1;
    public int MaxTps { get; set; } = 100;
    public int MaxSessions { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public int WindowSize { get; set; } = 50;
}

public class RouteStrategy
{
    private readonly Dictionary<string, SessionPool> _pools = new();
    private readonly object _lock = new();
    private readonly ILogger<RouteStrategy> _logger;
    private readonly RouteStrategyType _strategyType;
    private readonly Dictionary<string, int> _roundRobinCounters = new();

    public RouteStrategy(ILogger<RouteStrategy> logger, RouteStrategyType strategyType = RouteStrategyType.Weight)
    {
        _logger = logger;
        _strategyType = strategyType;
    }

    public void RegisterAccount(SmppAccount account)
    {
        lock (_lock)
        {
            if (!_pools.ContainsKey(account.Id))
            {
                var poolLogger = LoggerFactory.Create(b => b.AddConsole())
                    .CreateLogger<SessionPool>();
                _pools[account.Id] = new SessionPool(account.Id, account.MaxSessions, poolLogger);
                _roundRobinCounters[account.Id] = 0;
                _logger.LogInformation("Registered account: {AccountId} ({Name})", account.Id, account.Name);
            }
        }
    }

    public void UnregisterAccount(string accountId)
    {
        lock (_lock)
        {
            if (_pools.TryGetValue(accountId, out var pool))
            {
                pool.CloseAll();
                _pools.Remove(accountId);
                _roundRobinCounters.Remove(accountId);
                _logger.LogInformation("Unregistered account: {AccountId}", accountId);
            }
        }
    }

    public void AddSession(string accountId, Connection.Session session)
    {
        lock (_lock)
        {
            if (_pools.TryGetValue(accountId, out var pool))
            {
                pool.Add(session);
            }
        }
    }

    public Session? GetSession(string accountId)
    {
        lock (_lock)
        {
            if (_pools.TryGetValue(accountId, out var pool))
            {
                return _strategyType switch
                {
                    RouteStrategyType.Weight => pool.GetAvailable(),
                    RouteStrategyType.Priority => pool.GetAvailable(),
                    RouteStrategyType.LeastLoaded => pool.GetLeastLoaded(),
                    RouteStrategyType.RoundRobin => GetRoundRobinSession(accountId, pool),
                    _ => pool.GetAvailable()
                };
            }
        }
        return null;
    }

    private Session? GetRoundRobinSession(string accountId, SessionPool pool)
    {
        if (_roundRobinCounters.TryGetValue(accountId, out var counter))
        {
            var healthySessions = pool.GetHealthySessions().ToList();
            if (healthySessions.Count == 0) return null;

            var index = counter % healthySessions.Count;
            _roundRobinCounters[accountId] = (counter + 1) % int.MaxValue;
            return healthySessions[index];
        }
        return pool.GetAvailable();
    }

    public Session? GetBestSession()
    {
        lock (_lock)
        {
            SessionPool? bestPool = null;
            var bestScore = -1.0;

            foreach (var kvp in _pools)
            {
                var pool = kvp.Value;
                var healthyCount = pool.GetHealthySessions().Count();
                if (healthyCount == 0) continue;

                var score = CalculatePoolScore(pool);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPool = pool;
                }
            }

            if (bestPool == null) return null;

            return _strategyType switch
            {
                RouteStrategyType.Weight => bestPool.GetAvailable(),
                RouteStrategyType.Priority => bestPool.GetAvailable(),
                RouteStrategyType.LeastLoaded => bestPool.GetLeastLoaded(),
                RouteStrategyType.RoundRobin => GetRoundRobinSession(bestPool.AccountId, bestPool),
                _ => bestPool.GetAvailable()
            };
        }
    }

    private double CalculatePoolScore(SessionPool pool)
    {
        var healthyCount = pool.GetHealthySessions().Count();
        if (healthyCount == 0) return -1;

        var avgPending = pool.GetHealthySessions()
            .Select(s => s.WindowManager.PendingCount)
            .DefaultIfEmpty(0)
            .Average();

        var score = healthyCount * 1000 - avgPending;
        return score;
    }

    public SessionPool? GetPool(string accountId)
    {
        lock (_lock)
        {
            return _pools.GetValueOrDefault(accountId);
        }
    }

    public IEnumerable<SessionPool> GetAllPools()
    {
        lock (_lock)
        {
            return _pools.Values.ToList();
        }
    }

    public IEnumerable<string> GetAvailableAccountIds()
    {
        lock (_lock)
        {
            return _pools
                .Where(kvp => kvp.Value.SessionCount > 0)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }

    public bool HasAvailableSession => GetBestSession() != null;

    public int TotalHealthySessions
    {
        get
        {
            lock (_lock)
            {
                return _pools.Values.Sum(p => p.SessionCount);
            }
        }
    }
}
