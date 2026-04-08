using SmppStorage.Entities;
using SmppStorage.Repositories;

namespace SmppGateway.Services;

public interface IAlertService
{
    Task RecordAlertAsync(string accountId, AlertType type, AlertSeverity severity, string message, string? details = null);
    Task<List<AlertEntity>> GetUnresolvedAlertsAsync();
    Task<List<AlertEntity>> GetAlertsByAccountAsync(string accountId, int limit = 100);
    Task<List<AlertEntity>> GetAllAlertsAsync(int limit = 100);
    Task ResolveAlertAsync(Guid alertId);
    Task ResolveAlertsByAccountAsync(string accountId, AlertType type);
    event EventHandler<AlertEntity>? AlertCreated;
}

public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AlertService> _logger;
    private readonly List<AlertEntity> _cache = new();
    private readonly object _lock = new();
    private const int MaxCacheSize = 1000;

    public event EventHandler<AlertEntity>? AlertCreated;

    public AlertService(
        IAlertRepository alertRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<AlertService> logger)
    {
        _alertRepository = alertRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task RecordAlertAsync(string accountId, AlertType type, AlertSeverity severity, string message, string? details = null)
    {
        var alert = new AlertEntity
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Type = type,
            Severity = severity,
            Message = message,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _cache.Add(alert);
            if (_cache.Count > MaxCacheSize)
            {
                _cache.RemoveRange(0, _cache.Count - MaxCacheSize);
            }
        }

        try
        {
            await _alertRepository.CreateAsync(alert);

            await _auditLogRepository.CreateAsync(new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                Action = $"Alert_{type}",
                EntityType = "Channel",
                EntityId = accountId,
                Details = $"Severity: {severity}, Message: {message}, Details: {details}",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist alert to database, using cache only");
        }

        AlertCreated?.Invoke(this, alert);

        _logger.LogWarning("Alert created: [{Severity}] {Type} on {AccountId}: {Message}",
            severity, type, accountId, message);
    }

    public async Task<List<AlertEntity>> GetUnresolvedAlertsAsync()
    {
        try
        {
            return await _alertRepository.GetUnresolvedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get unresolved alerts from database, using cache");
            lock (_lock)
            {
                return _cache.Where(a => !a.IsResolved)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToList();
            }
        }
    }

    public async Task<List<AlertEntity>> GetAlertsByAccountAsync(string accountId, int limit = 100)
    {
        try
        {
            return await _alertRepository.GetByAccountIdAsync(accountId, limit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get alerts by account from database, using cache");
            lock (_lock)
            {
                return _cache.Where(a => a.AccountId == accountId)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(limit)
                    .ToList();
            }
        }
    }

    public async Task<List<AlertEntity>> GetAllAlertsAsync(int limit = 100)
    {
        try
        {
            return await _alertRepository.GetAllAsync(limit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get all alerts from database, using cache");
            lock (_lock)
            {
                return _cache.OrderByDescending(a => a.CreatedAt)
                    .Take(limit)
                    .ToList();
            }
        }
    }

    public async Task ResolveAlertAsync(Guid alertId)
    {
        lock (_lock)
        {
            var alert = _cache.FirstOrDefault(a => a.Id == alertId);
            if (alert != null)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
            }
        }

        try
        {
            await _alertRepository.ResolveAsync(alertId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve alert in database");
        }
    }

    public async Task ResolveAlertsByAccountAsync(string accountId, AlertType type)
    {
        lock (_lock)
        {
            foreach (var alert in _cache.Where(a => a.AccountId == accountId && a.Type == type && !a.IsResolved))
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
            }
        }

        try
        {
            await _alertRepository.ResolveByAccountAsync(accountId, type);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve alerts in database");
        }
    }
}
