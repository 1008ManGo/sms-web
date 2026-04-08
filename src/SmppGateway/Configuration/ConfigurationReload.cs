using System.Text.Json;
using SmppClient.Services;
using SmppClient.Routing;
using SmppGateway.Services;

namespace SmppGateway.Configuration;

public interface IConfigurationChangeNotifier
{
    event EventHandler<AppConfig>? ConfigurationChanged;
    void StartWatching(string configPath);
    void StopWatching();
}

public class ConfigurationReloadService : IConfigurationChangeNotifier, IDisposable
{
    private readonly ILogger<ConfigurationReloadService> _logger;
    private FileSystemWatcher? _watcher;
    private readonly object _lock = new();
    private bool _disposed;
    private AppConfig? _lastConfig;

    public event EventHandler<AppConfig>? ConfigurationChanged;

    public ConfigurationReloadService(ILogger<ConfigurationReloadService> logger)
    {
        _logger = logger;
    }

    public void StartWatching(string configPath)
    {
        if (_disposed) return;

        var directory = Path.GetDirectoryName(configPath);
        var fileName = Path.GetFileName(configPath);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
        {
            _logger.LogWarning("Invalid config path: {ConfigPath}", configPath);
            return;
        }

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnConfigFileChanged;
        _logger.LogInformation("Started watching config file: {ConfigPath}", configPath);
    }

    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnConfigFileChanged;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Config file changed: {FullPath}", e.FullPath);

        Task.Delay(100).ContinueWith(_ =>
        {
            try
            {
                LoadAndNotifyConfig(e.FullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload configuration from {Path}", e.FullPath);
            }
        });
    }

    private void LoadAndNotifyConfig(string configPath)
    {
        if (!File.Exists(configPath))
        {
            _logger.LogWarning("Config file no longer exists: {ConfigPath}", configPath);
            return;
        }

        var json = File.ReadAllText(configPath);
        var newConfig = JsonSerializer.Deserialize<AppConfig>(json);

        if (newConfig == null)
        {
            _logger.LogWarning("Failed to deserialize config file");
            return;
        }

        lock (_lock)
        {
            if (_lastConfig != null && newConfig.Equals(_lastConfig))
            {
                _logger.LogDebug("Config unchanged, skipping notification");
                return;
            }

            _lastConfig = newConfig;
        }

        _logger.LogInformation("Configuration reloaded successfully");
        ConfigurationChanged?.Invoke(this, newConfig);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopWatching();
    }
}

public class DynamicConfigService
{
    private readonly AppConfig _config;
    private readonly IConfigurationChangeNotifier _changeNotifier;
    private readonly ILogger<DynamicConfigService> _logger;
    private readonly List<IConfigChangeHandler> _handlers = new();

    public DynamicConfigService(
        AppConfig config,
        IConfigurationChangeNotifier changeNotifier,
        ILogger<DynamicConfigService> logger)
    {
        _config = config;
        _changeNotifier = changeNotifier;
        _logger = logger;

        _changeNotifier.ConfigurationChanged += OnConfigurationChanged;
    }

    public void RegisterHandler(IConfigChangeHandler handler)
    {
        _handlers.Add(handler);
    }

    private async void OnConfigurationChanged(object? sender, AppConfig newConfig)
    {
        _logger.LogInformation("Processing configuration changes...");

        foreach (var handler in _handlers)
        {
            try
            {
                await handler.HandleConfigChangeAsync(newConfig);
                _logger.LogInformation("Applied config change: {HandlerType}", handler.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply config change for {HandlerType}", handler.GetType().Name);
            }
        }
    }
}

public interface IConfigChangeHandler
{
    Task HandleConfigChangeAsync(AppConfig newConfig);
}

public class AccountConfigChangeHandler : IConfigChangeHandler
{
    private readonly Services.ISmppClientManager _clientManager;
    private readonly ILogger<AccountConfigChangeHandler> _logger;

    public AccountConfigChangeHandler(
        Services.ISmppClientManager clientManager,
        ILogger<AccountConfigChangeHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task HandleConfigChangeAsync(AppConfig newConfig)
    {
        foreach (var account in newConfig.Accounts)
        {
            if (account.Enabled)
            {
                if (account.MaxTps > 0)
                {
                    await _clientManager.UpdateAccountTpsAsync(account.Id, account.MaxTps);
                    _logger.LogInformation("Updated TPS for account {AccountId}: {MaxTps}", account.Id, account.MaxTps);
                }

                if (account.MaxSessions > 0)
                {
                    await _clientManager.UpdateAccountSessionsAsync(account.Id, account.MaxSessions);
                    _logger.LogInformation("Updated sessions for account {AccountId}: {MaxSessions}", account.Id, account.MaxSessions);
                }
            }
        }
    }
}

public interface ISmppClientManager
{
    Task StartAsync();
    Task StopAsync();
    SubmitService GetSubmitService();
    DlrProcessor GetDlrProcessor();
    RouteStrategy GetRouteStrategy();
    int TotalSessions { get; }
    int HealthySessions { get; }
    void SetAlertService(IAlertService alertService);
    Task<bool> UpdateAccountSessionsAsync(string accountId, int newMaxSessions);
    Task<bool> AddSessionAsync(string accountId);
    Task<bool> RemoveSessionAsync(string accountId);
    Task<bool> UpdateAccountTpsAsync(string accountId, int newMaxTps);
    Dictionary<string, List<string>> GetAccountSessions();
}
