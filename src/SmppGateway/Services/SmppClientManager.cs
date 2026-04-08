using SmppClient;
using SmppClient.Connection;
using SmppClient.Queue;
using SmppClient.Routing;
using SmppClient.Services;
using SmppGateway.Configuration;
using SmppStorage.Entities;

namespace SmppGateway.Services;

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

public class SmppClientManager : ISmppClientManager, IDisposable
{
    private readonly AppConfig _config;
    private readonly ILogger<SmppClientManager> _logger;
    private readonly RouteStrategy _routeStrategy;
    private readonly LongMessageProcessor _longMessageProcessor;
    private readonly DlrProcessor _dlrProcessor;
    private readonly SubmitService _submitService;
    private readonly IQueueAdapter _queueAdapter;
    private readonly Dictionary<string, List<ConnectionManager>> _accountConnections = new();
    private readonly Dictionary<string, SmppAccount> _accountConfigs = new();
    private readonly object _lock = new();
    private IAlertService? _alertService;
    private bool _disposed;

    public SmppClientManager(
        AppConfig config,
        ILogger<SmppClientManager> logger)
    {
        _config = config;
        _logger = logger;

        _routeStrategy = new RouteStrategy(
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<RouteStrategy>());

        _longMessageProcessor = new LongMessageProcessor();

        _dlrProcessor = new DlrProcessor(
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<DlrProcessor>());

        _queueAdapter = new RabbitMqAdapter(
            _config.RabbitMq.Host,
            _config.RabbitMq.Port,
            _config.RabbitMq.Username,
            _config.RabbitMq.Password,
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<RabbitMqAdapter>());

        _submitService = new SubmitService(
            _routeStrategy,
            _longMessageProcessor,
            _dlrProcessor,
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SubmitService>(),
            _queueAdapter);
    }

    public async Task StartAsync()
    {
        foreach (var accountConfig in _config.Accounts.Where(a => a.Enabled))
        {
            var smppAccount = new SmppAccount
            {
                Id = accountConfig.Id,
                Name = accountConfig.Name,
                Host = accountConfig.Host,
                Port = accountConfig.Port,
                SystemId = accountConfig.SystemId,
                Password = accountConfig.Password,
                SystemType = accountConfig.SystemType,
                Weight = accountConfig.Weight,
                Priority = accountConfig.Priority,
                MaxTps = accountConfig.MaxTps,
                MaxSessions = accountConfig.MaxSessions
            };

            _routeStrategy.RegisterAccount(smppAccount);
            _accountConfigs[accountConfig.Id] = accountConfig;
            _accountConnections[accountConfig.Id] = new List<ConnectionManager>();

            for (int i = 0; i < accountConfig.MaxSessions; i++)
            {
                await CreateConnectionAsync(accountConfig, accountConfig.Id);
            }
        }

        await _queueAdapter.InitializeAsync();
        _logger.LogInformation("SMPP Client Manager started");
    }

    private async Task CreateConnectionAsync(SmppClient.Routing.SmppAccount accountConfig, string? specificAccountId = null)
    {
        var accountId = specificAccountId ?? accountConfig.Id;
        var connectionConfig = new ConnectionConfig
        {
            Host = accountConfig.Host,
            Port = accountConfig.Port,
            SystemId = accountConfig.SystemId,
            Password = accountConfig.Password,
            SystemType = accountConfig.SystemType,
            WindowSize = 50,
            EnquireLinkInterval = TimeSpan.FromSeconds(30)
        };

        var connectionManager = new ConnectionManager(
            connectionConfig,
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ConnectionManager>());

        connectionManager.ConnectionLost += (_, ex) =>
        {
            _logger.LogWarning(ex, "Connection lost for account {AccountId}", accountId);
            _alertService?.RecordAlertAsync(accountId, AlertType.ConnectionLost,
                AlertSeverity.Warning, $"Connection lost: {ex.Message}", ex.ToString());
            _ = TryReconnectAsync(connectionManager, connectionConfig, accountId);
        };

        lock (_lock)
        {
            if (_accountConnections.TryGetValue(accountId, out var list))
            {
                list.Add(connectionManager);
            }
            else
            {
                _accountConnections[accountId] = new List<ConnectionManager> { connectionManager };
            }
        }

        try
        {
            await connectionManager.ConnectAsync();
            var session = CreateSessionFromConnection(connectionManager);
            _routeStrategy.AddSession(accountId, session);
            _logger.LogInformation("Connected to {AccountId}, total sessions: {Count}", 
                accountId, _accountConnections[accountId].Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {AccountId}", accountId);
        }
    }

    public async Task<bool> AddSessionAsync(string accountId)
    {
        lock (_lock)
        {
            if (!_accountConfigs.TryGetValue(accountId, out var accountConfig))
            {
                _logger.LogWarning("Account {AccountId} not found", accountId);
                return false;
            }

            var currentCount = _accountConnections.TryGetValue(accountId, out var list) 
                ? list.Count(s => s.IsConnected) 
                : 0;

            if (currentCount >= accountConfig.MaxSessions)
            {
                _logger.LogWarning("Account {AccountId} already at max sessions ({Max})", accountId, accountConfig.MaxSessions);
                return false;
            }
        }

        await CreateConnectionAsync(accountConfig, accountId);
        return true;
    }

    public async Task<bool> RemoveSessionAsync(string accountId)
    {
        List<ConnectionManager>? connections;
        lock (_lock)
        {
            if (!_accountConnections.TryGetValue(accountId, out connections) || connections.Count == 0)
            {
                _logger.LogWarning("No sessions to remove for account {AccountId}", accountId);
                return false;
            }

            var connected = connections.FirstOrDefault(c => c.IsConnected);
            if (connected != null)
            {
                connections.Remove(connected);
                connected.Dispose();
                _logger.LogInformation("Removed session from account {AccountId}, remaining: {Count}", 
                    accountId, connections.Count);
                return true;
            }
        }

        return false;
    }

    public async Task<bool> UpdateAccountSessionsAsync(string accountId, int newMaxSessions)
    {
        if (!_accountConfigs.TryGetValue(accountId, out var accountConfig))
        {
            _logger.LogWarning("Account {AccountId} not found", accountId);
            return false;
        }

        var currentSessions = _accountConnections.TryGetValue(accountId, out var conns) 
            ? conns.Count(s => s.IsConnected) 
            : 0;

        _accountConfigs[accountId] = new AccountConfig
        {
            Id = accountConfig.Id,
            Name = accountConfig.Name,
            Host = accountConfig.Host,
            Port = accountConfig.Port,
            SystemId = accountConfig.SystemId,
            Password = accountConfig.Password,
            SystemType = accountConfig.SystemType,
            Weight = accountConfig.Weight,
            Priority = accountConfig.Priority,
            MaxTps = accountConfig.MaxTps,
            MaxSessions = newMaxSessions,
            Enabled = accountConfig.Enabled
        };

        if (newMaxSessions > currentSessions)
        {
            for (int i = 0; i < newMaxSessions - currentSessions; i++)
            {
                await CreateConnectionAsync(_accountConfigs[accountId], accountId);
            }
            _logger.LogInformation("Increased sessions for {AccountId} from {Old} to {New}", 
                accountId, currentSessions, newMaxSessions);
        }
        else if (newMaxSessions < currentSessions)
        {
            var toRemove = currentSessions - newMaxSessions;
            for (int i = 0; i < toRemove; i++)
            {
                await RemoveSessionAsync(accountId);
            }
            _logger.LogInformation("Decreased sessions for {AccountId} from {Old} to {New}", 
                accountId, currentSessions, newMaxSessions);
        }

        return true;
    }

    public async Task<bool> UpdateAccountTpsAsync(string accountId, int newMaxTps)
    {
        if (!_accountConfigs.TryGetValue(accountId, out var accountConfig))
        {
            _logger.LogWarning("Account {AccountId} not found", accountId);
            return false;
        }

        _accountConfigs[accountId] = new AccountConfig
        {
            Id = accountConfig.Id,
            Name = accountConfig.Name,
            Host = accountConfig.Host,
            Port = accountConfig.Port,
            SystemId = accountConfig.SystemId,
            Password = accountConfig.Password,
            SystemType = accountConfig.SystemType,
            Weight = accountConfig.Weight,
            Priority = accountConfig.Priority,
            MaxTps = newMaxTps,
            MaxSessions = accountConfig.MaxSessions,
            Enabled = accountConfig.Enabled
        };

        _logger.LogInformation("Updated TPS for {AccountId} to {NewTps}", accountId, newMaxTps);
        await Task.CompletedTask;
        return true;
    }

    public Dictionary<string, List<string>> GetAccountSessions()
    {
        lock (_lock)
        {
            return _accountConnections.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(c => c.IsConnected ? "connected" : "disconnected").ToList()
            );
        }
    }

    private async Task TryReconnectAsync(ConnectionManager connectionManager, ConnectionConfig config, string accountId)
    {
        var retryCount = 0;
        var maxRetries = 10;

        while (retryCount < maxRetries)
        {
            try
            {
                var delay = Math.Min(1000 * Math.Pow(2, retryCount), 30000);
                await Task.Delay((int)delay);
                await connectionManager.ConnectAsync();

                var session = CreateSessionFromConnection(connectionManager);
                _routeStrategy.AddSession(accountId, session);

                await _alertService?.ResolveAlertsByAccountAsync(accountId, AlertType.ConnectionLost)!;
                await _alertService?.RecordAlertAsync(accountId, AlertType.ReconnectSuccess, 
                    AlertSeverity.Info, "Channel reconnected successfully")!;

                _logger.LogInformation("Reconnected successfully");
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Reconnect failed, attempt {Attempt}/{Max}", retryCount, maxRetries);

                if (retryCount == 3)
                {
                    await _alertService?.RecordAlertAsync(accountId, AlertType.ConnectionLost,
                        AlertSeverity.Warning, $"Connection lost, retrying ({retryCount}/{maxRetries})", ex.Message)!;
                }

                if (retryCount >= maxRetries)
                {
                    await _alertService?.RecordAlertAsync(accountId, AlertType.ReconnectFailed,
                        AlertSeverity.Critical, $"Failed to reconnect after {maxRetries} attempts", ex.Message)!;
                }
            }
        }

        _logger.LogError("Failed to reconnect after {MaxRetries} attempts", maxRetries);
    }

    private Session CreateSessionFromConnection(ConnectionManager connectionManager)
    {
        var session = connectionManager.CreateSession();
        if (session == null)
            throw new InvalidOperationException("Failed to create session from connection manager");
        session.ConnectionLost += (_, ex) =>
        {
            _logger.LogWarning(ex, "Session connection lost");
        };
        return session;
    }

    public async Task StopAsync()
    {
        lock (_lock)
        {
            foreach (var connections in _accountConnections.Values)
            {
                foreach (var cm in connections)
                {
                    cm.Dispose();
                }
            }
            _accountConnections.Clear();
        }
        _queueAdapter.Dispose();
        _logger.LogInformation("SMPP Client Manager stopped");
    }

    public SubmitService GetSubmitService() => _submitService;
    public DlrProcessor GetDlrProcessor() => _dlrProcessor;
    public RouteStrategy GetRouteStrategy() => _routeStrategy;

    public int TotalSessions => _routeStrategy.TotalHealthySessions;

    public int HealthySessions
    {
        get
        {
            var count = 0;
            foreach (var pool in _routeStrategy.GetAllPools())
            {
                count += pool.SessionCount;
            }
            return count;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopAsync().Wait();
    }

    public void SetAlertService(IAlertService alertService)
    {
        _alertService = alertService;
    }
}
