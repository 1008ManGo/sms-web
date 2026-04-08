using Microsoft.Extensions.Logging;
using SmppClient.Connection;

namespace SmppClient.Routing;

public class SessionPool
{
    private readonly List<Session> _sessions = new();
    private readonly object _lock = new();
    private readonly ILogger<SessionPool> _logger;

    public string AccountId { get; }
    public int MaxSessions { get; }

    public SessionPool(string accountId, int maxSessions, ILogger<SessionPool> logger)
    {
        AccountId = accountId;
        MaxSessions = maxSessions;
        _logger = logger;
    }

    public int SessionCount
    {
        get
        {
            lock (_lock)
            {
                return _sessions.Count(s => s.IsConnected && s.IsBound);
            }
        }
    }

    public int TotalSessionCount
    {
        get
        {
            lock (_lock)
            {
                return _sessions.Count;
            }
        }
    }

    public void Add(Session session)
    {
        lock (_lock)
        {
            if (_sessions.Count >= MaxSessions)
            {
                _logger.LogWarning("Session pool {AccountId} is full ({Max}), cannot add session",
                    AccountId, MaxSessions);
                return;
            }
            _sessions.Add(session);
            _logger.LogInformation("Added session to pool {AccountId}, total: {Count}",
                AccountId, _sessions.Count);
        }
    }

    public void Remove(Session session)
    {
        lock (_lock)
        {
            _sessions.Remove(session);
            _logger.LogInformation("Removed session from pool {AccountId}, remaining: {Count}",
                AccountId, _sessions.Count);
        }
    }

    public Session? GetAvailable()
    {
        lock (_lock)
        {
            foreach (var session in _sessions)
            {
                if (session.IsConnected && session.IsBound && !session.WindowManager.IsFull)
                    return session;
            }
            return null;
        }
    }

    public IEnumerable<Session> GetAllSessions()
    {
        lock (_lock)
        {
            return _sessions.ToList();
        }
    }

    public IEnumerable<Session> GetHealthySessions()
    {
        lock (_lock)
        {
            return _sessions.Where(s => s.IsConnected && s.IsBound).ToList();
        }
    }

    public Session? GetLeastLoaded()
    {
        lock (_lock)
        {
            Session? leastLoaded = null;
            var minPending = int.MaxValue;

            foreach (var session in _sessions)
            {
                if (session.IsConnected && session.IsBound)
                {
                    var pending = session.WindowManager.PendingCount;
                    if (pending < minPending)
                    {
                        minPending = pending;
                        leastLoaded = session;
                    }
                }
            }

            return leastLoaded;
        }
    }

    public void CloseAll()
    {
        lock (_lock)
        {
            foreach (var session in _sessions)
            {
                try
                {
                    session.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing session");
                }
            }
            _sessions.Clear();
        }
    }

    public bool HasAvailableSession => GetAvailable() != null;
}
