using SmppClient;
using SmppClient.Connection;
using SmppClient.Queue;
using SmppClient.Routing;
using SmppClient.Services;
using SmppGateway.Configuration;

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
    private readonly List<ConnectionManager> _connectionManagers = new();
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
            var account = new SmppAccount
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

            _routeStrategy.RegisterAccount(account);

            for (int i = 0; i < accountConfig.MaxSessions; i++)
            {
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
                    _logger.LogWarning(ex, "Connection lost for account {AccountId}", accountConfig.Id);
                    _ = TryReconnectAsync(connectionManager, connectionConfig);
                };

                _connectionManagers.Add(connectionManager);

                try
                {
                    await connectionManager.ConnectAsync();
                    var session = CreateSessionFromConnection(connectionManager);
                    _routeStrategy.AddSession(accountConfig.Id, session);
                    _logger.LogInformation("Connected to {AccountId} session {Index}", accountConfig.Id, i);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to {AccountId}", accountConfig.Id);
                }
            }
        }

        await _queueAdapter.InitializeAsync();
        _logger.LogInformation("SMPP Client Manager started");
    }

    private async Task TryReconnectAsync(ConnectionManager connectionManager, ConnectionConfig config)
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
                _logger.LogInformation("Reconnected successfully");
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Reconnect failed, attempt {Attempt}/{Max}", retryCount, maxRetries);
            }
        }

        _logger.LogError("Failed to reconnect after {MaxRetries} attempts", maxRetries);
    }

    private static Connection.Session CreateSessionFromConnection(ConnectionManager connectionManager)
    {
        throw new NotImplementedException("Session access requires internal API");
    }

    public async Task StopAsync()
    {
        foreach (var cm in _connectionManagers)
        {
            cm.Dispose();
        }
        _connectionManagers.Clear();
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
}
