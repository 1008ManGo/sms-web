using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SmppClient.Services;

public interface IIdempotencyService
{
    bool TryGet(string key, out SubmitResult? result);
    void Set(string key, SubmitResult result, TimeSpan expiry);
    void Remove(string key);
}

public class InMemoryIdempotencyService : IIdempotencyService
{
    private readonly ConcurrentDictionary<string, (SubmitResult Result, DateTime Expiry)> _store = new();
    private readonly Timer _cleanupTimer;

    public InMemoryIdempotencyService()
    {
        _cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public bool TryGet(string key, out SubmitResult? result)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.Expiry > DateTime.UtcNow)
            {
                result = entry.Result;
                result!.IsDuplicate = true;
                return true;
            }
            _store.TryRemove(key, out _);
        }
        result = null;
        return false;
    }

    public void Set(string key, SubmitResult result, TimeSpan expiry)
    {
        var entry = (result, DateTime.UtcNow.Add(expiry));
        _store[key] = entry;
    }

    public void Remove(string key)
    {
        _store.TryRemove(key, out _);
    }

    private void Cleanup(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _store.Where(x => x.Value.Expiry <= now))
        {
            _store.TryRemove(kvp.Key, out _);
        }
    }
}

public class IdempotencyService : IIdempotencyService
{
    private readonly IIdempotencyStore _store;
    private readonly TimeSpan _defaultExpiry;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(
        IIdempotencyStore store,
        ILogger<IdempotencyService> logger,
        TimeSpan? defaultExpiry = null)
    {
        _store = store;
        _logger = logger;
        _defaultExpiry = defaultExpiry ?? TimeSpan.FromHours(24);
    }

    public bool TryGet(string key, out SubmitResult? result)
    {
        try
        {
            return _store.TryGet(key, out result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get idempotency key {Key}", key);
            result = null;
            return false;
        }
    }

    public void Set(string key, SubmitResult result, TimeSpan expiry)
    {
        try
        {
            _store.Set(key, result, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set idempotency key {Key}", key);
        }
    }

    public void Remove(string key)
    {
        try
        {
            _store.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove idempotency key {Key}", key);
        }
    }
}

public interface IIdempotencyStore
{
    bool TryGet(string key, out SubmitResult? result);
    void Set(string key, SubmitResult result, TimeSpan expiry);
    void Remove(string key);
}

public class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly string _connectionString;
    private readonly ILogger<RedisIdempotencyStore> _logger;

    public RedisIdempotencyStore(string connectionString, ILogger<RedisIdempotencyStore> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public bool TryGet(string key, out SubmitResult? result)
    {
        throw new NotImplementedException("Redis implementation requires StackExchange.Redis package");
    }

    public void Set(string key, SubmitResult result, TimeSpan expiry)
    {
        throw new NotImplementedException("Redis implementation requires StackExchange.Redis package");
    }

    public void Remove(string key)
    {
        throw new NotImplementedException("Redis implementation requires StackExchange.Redis package");
    }
}
