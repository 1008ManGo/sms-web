using System.Net.Sockets;
using SmppClient.Core;
using SmppClient.Protocol;

namespace SmppClient.Connection;

public class ConnectionConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 2775;
    public string SystemId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SystemType { get; set; } = "SMPP";
    public int ConnectionTimeout { get; set; } = 30000;
    public int WindowSize { get; set; } = 50;
    public TimeSpan EnquireLinkInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan EnquireLinkTimeout { get; set; } = TimeSpan.FromSeconds(10);
}

public class ConnectionManager : IDisposable
{
    private readonly ConnectionConfig _config;
    private readonly PduCodec _codec;
    private readonly SequenceManager _sequenceManager;
    private readonly ILogger<ConnectionManager> _logger;
    private readonly object _lock = new();

    private TcpClient? _tcpClient;
    private Session? _session;
    private WindowManager? _windowManager;
    private bool _disposed;
    private Task? _enquireLinkTask;
    private CancellationTokenSource? _linkCts;

    public ConnectionManager(ConnectionConfig config, ILogger<ConnectionManager> logger)
    {
        _config = config;
        _codec = new PduCodec();
        _sequenceManager = new SequenceManager();
        _logger = logger;
    }

    public bool IsConnected => _session?.IsConnected ?? false;
    public bool IsBound => _session?.IsBound ?? false;
    public TcpClient? TcpClient => _tcpClient;
    public WindowManager? WindowManager => _windowManager;
    public SequenceManager SequenceManager => _sequenceManager;
    public PduCodec PduCodec => _codec;

    public event EventHandler<Exception>? ConnectionLost;

    public async Task ConnectAsync()
    {
        lock (_lock)
        {
            if (_session?.IsConnected == true)
                return;
        }

        var tcpClient = new TcpClient();
        using var timeoutCts = new CancellationTokenSource(_config.ConnectionTimeout);

        try
        {
            await tcpClient.ConnectAsync(_config.Host, _config.Port, timeoutCts.Token);
            _tcpClient = tcpClient;

            _windowManager = new WindowManager(_config.WindowSize, 
                LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WindowManager>());

            _session = new Session(
                Guid.NewGuid().ToString(),
                tcpClient,
                _codec,
                _sequenceManager,
                _windowManager,
                LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Session>());

            _session.ConnectionLost += (_, ex) => ConnectionLost?.Invoke(this, ex);

            await _session.StartAsync();
            await _session.BindTransceiverAsync(_config.SystemId, _config.Password, _config.SystemType);

            _logger.LogInformation("Connected to {Host}:{Port}", _config.Host, _config.Port);

            StartEnquireLink();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Host}:{Port}", _config.Host, _config.Port);
            tcpClient.Dispose();
            throw;
        }
    }

    public Session? CreateSession()
    {
        if (_tcpClient == null || _windowManager == null)
            return null;

        return new Session(
            Guid.NewGuid().ToString(),
            _tcpClient,
            _codec,
            _sequenceManager,
            _windowManager,
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Session>());
    }

    private void StartEnquireLink()
    {
        _linkCts = new CancellationTokenSource();
        _enquireLinkTask = EnquireLinkLoopAsync(_linkCts.Token);
    }

    private async Task EnquireLinkLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.EnquireLinkInterval, cancellationToken);

                if (!IsBound)
                    continue;

                var request = new EnquireLinkPdu
                {
                    SequenceNumber = _sequenceManager.Next()
                };

                var response = await _session!.SendRequestAsync(request, _config.EnquireLinkTimeout);
                _logger.LogDebug("EnquireLink response received, seq: {Sequence}", response.SequenceNumber);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EnquireLink failed");
                ConnectionLost?.Invoke(this, ex);
                break;
            }
        }
    }

    public async Task<Pdu> SendRequestAsync(Pdu request, TimeSpan timeout)
    {
        if (_session == null)
            throw new InvalidOperationException("Not connected");

        return await _session.SendRequestAsync(request, timeout);
    }

    public async Task ReconnectAsync()
    {
        _logger.LogInformation("Attempting to reconnect...");
        await Task.Delay(1000);
        await ConnectAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _linkCts?.Cancel();
        _linkCts?.Dispose();
        _enquireLinkTask?.Wait(TimeSpan.FromSeconds(5));

        _session?.Dispose();
    }
}
