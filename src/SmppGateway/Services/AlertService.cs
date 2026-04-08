using SmppStorage.Entities;
using SmppStorage.Repositories;

namespace SmppGateway.Services;

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum AlertType
{
    ChannelUp = 0,
    ChannelDown = 1,
    HighLoad = 2,
    QueueBacklog = 3,
    ConnectionLost = 4,
    ReconnectSuccess = 5,
    ReconnectFailed = 6
}

public class AlertEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string AccountId { get; set; } = string.Empty;

    public AlertType Type { get; set; }

    public AlertSeverity Severity { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedAt { get; set; }
}

public interface IAlertService
{
    Task RecordAlertAsync(string accountId, AlertType type, AlertSeverity severity, string message, string? details = null);
    Task<List<AlertEntity>> GetUnresolvedAlertsAsync();
    Task<List<AlertEntity>> GetAlertsByAccountAsync(string accountId, int limit = 100);
    Task ResolveAlertAsync(Guid alertId);
    Task ResolveAlertsByAccountAsync(string accountId, AlertType type);
    event EventHandler<AlertEntity>? AlertCreated;
}

public class AlertService : IAlertService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AlertService> _logger;
    private readonly List<AlertEntity> _alerts = new();
    private readonly object _lock = new();

    public event EventHandler<AlertEntity>? AlertCreated;

    public AlertService(IAuditLogRepository auditLogRepository, ILogger<AlertService> logger)
    {
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
            _alerts.Add(alert);
            if (_alerts.Count > 1000)
            {
                _alerts.RemoveRange(0, _alerts.Count - 1000);
            }
        }

        try
        {
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
            _logger.LogWarning(ex, "Failed to record alert to audit log");
        }

        AlertCreated?.Invoke(this, alert);

        _logger.LogWarning("Alert created: [{Severity}] {Type} on {AccountId}: {Message}",
            severity, type, accountId, message);
    }

    public Task<List<AlertEntity>> GetUnresolvedAlertsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_alerts.Where(a => !a.IsResolved)
                .OrderByDescending(a => a.CreatedAt)
                .ToList());
        }
    }

    public Task<List<AlertEntity>> GetAlertsByAccountAsync(string accountId, int limit = 100)
    {
        lock (_lock)
        {
            return Task.FromResult(_alerts.Where(a => a.AccountId == accountId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToList());
        }
    }

    public Task ResolveAlertAsync(Guid alertId)
    {
        lock (_lock)
        {
            var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
            if (alert != null)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
            }
        }
        return Task.CompletedTask;
    }

    public Task ResolveAlertsByAccountAsync(string accountId, AlertType type)
    {
        lock (_lock)
        {
            foreach (var alert in _alerts.Where(a => a.AccountId == accountId && a.Type == type && !a.IsResolved))
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
            }
        }
        return Task.CompletedTask;
    }
}
