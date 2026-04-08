using System.Net.Http.Json;
using System.Text.Json;
using SmppStorage.Entities;

namespace SmppGateway.Services;

public interface IWebhookNotificationService
{
    Task SendAlertNotificationAsync(AlertEntity alert);
    Task SendBatchAlertNotificationAsync(IEnumerable<AlertEntity> alerts);
    void ConfigureWebhook(string url, Dictionary<string, string>? headers = null);
    void EnableWebhook();
    void DisableWebhook();
    bool IsEnabled { get; }
}

public class WebhookNotificationService : IWebhookNotificationService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookNotificationService> _logger;
    private string? _webhookUrl;
    private Dictionary<string, string>? _webhookHeaders;
    private bool _enabled;
    private readonly object _lock = new();
    private readonly Queue<(AlertEntity Alert, DateTime RetryAfter)> _retryQueue = new();
    private Timer? _retryTimer;

    public bool IsEnabled => _enabled;

    public WebhookNotificationService(ILogger<WebhookNotificationService> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;
        _retryTimer = new Timer(ProcessRetryQueue, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void ConfigureWebhook(string url, Dictionary<string, string>? headers = null)
    {
        lock (_lock)
        {
            _webhookUrl = url;
            _webhookHeaders = headers;
            _logger.LogInformation("Webhook configured: {Url}", url);
        }
    }

    public void EnableWebhook()
    {
        lock (_lock)
        {
            _enabled = true;
            _logger.LogInformation("Webhook enabled");
        }
    }

    public void DisableWebhook()
    {
        lock (_lock)
        {
            _enabled = false;
            _logger.LogInformation("Webhook disabled");
        }
    }

    public async Task SendAlertNotificationAsync(AlertEntity alert)
    {
        if (!_enabled || string.IsNullOrEmpty(_webhookUrl))
            return;

        var payload = new
        {
            type = "alert",
            timestamp = DateTime.UtcNow,
            alert = new
            {
                id = alert.Id,
                accountId = alert.AccountId,
                type = alert.Type.ToString(),
                severity = alert.Severity.ToString(),
                message = alert.Message,
                details = alert.Details,
                createdAt = alert.CreatedAt,
                isResolved = alert.IsResolved
            }
        };

        await SendWebhookAsync(payload);
    }

    public async Task SendBatchAlertNotificationAsync(IEnumerable<AlertEntity> alerts)
    {
        if (!_enabled || string.IsNullOrEmpty(_webhookUrl))
            return;

        var alertList = alerts.ToList();
        var payload = new
        {
            type = "batch_alerts",
            timestamp = DateTime.UtcNow,
            count = alertList.Count,
            alerts = alertList.Select(a => new
            {
                id = a.Id,
                accountId = a.AccountId,
                type = a.Type.ToString(),
                severity = a.Severity.ToString(),
                message = a.Message,
                createdAt = a.CreatedAt
            })
        };

        await SendWebhookAsync(payload);
    }

    private async Task SendWebhookAsync(object payload)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
            return;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _webhookUrl);
            request.Content = JsonContent.Create(payload);

            if (_webhookHeaders != null)
            {
                foreach (var header in _webhookHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Webhook sent successfully");
            }
            else
            {
                _logger.LogWarning("Webhook returned {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send webhook");
        }
    }

    private async void ProcessRetryQueue(object? state)
    {
        List<AlertEntity>? alertsToRetry = null;

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            while (_retryQueue.Count > 0 && _retryQueue.Peek().RetryAfter <= now)
            {
                alertsToRetry ??= new List<AlertEntity>();
                var (alert, _) = _retryQueue.Dequeue();
                alertsToRetry.Add(alert);
            }
        }

        if (alertsToRetry != null)
        {
            foreach (var alert in alertsToRetry)
            {
                await SendAlertNotificationAsync(alert);
            }
        }
    }

    public void Dispose()
    {
        _retryTimer?.Dispose();
        _httpClient.Dispose();
    }
}

public class AlertWebhookConfig
{
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public bool Enabled { get; set; } = false;
    public int RetryCount { get; set; } = 3;
    public int RetryIntervalSeconds { get; set; } = 30;
}

public class AlertNotificationService : IAlertNotificationService
{
    private readonly IAlertService _alertService;
    private readonly IWebhookNotificationService _webhookService;
    private readonly ILogger<AlertNotificationService> _logger;

    public AlertNotificationService(
        IAlertService alertService,
        IWebhookNotificationService webhookService,
        ILogger<AlertNotificationService> logger)
    {
        _alertService = alertService;
        _webhookService = webhookService;
        _logger = logger;

        _alertService.AlertCreated += OnAlertCreated;
    }

    private async void OnAlertCreated(object? sender, AlertEntity alert)
    {
        try
        {
            if (alert.Severity == AlertSeverity.Critical || alert.Severity == AlertSeverity.Warning)
            {
                await _webhookService.SendAlertNotificationAsync(alert);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send alert notification for {AlertId}", alert.Id);
        }
    }
}

public interface IAlertNotificationService
{
}
