using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SmppClient.Core;
using SmppClient.Protocol;

namespace SmppClient.Connection;

public static class SessionFactory
{
    private static readonly PduCodec DefaultCodec = new();
    private static readonly ILoggerFactory DefaultLoggerFactory = LoggerFactory.Create(b => b.AddConsole());

    public static Session CreateFromConnectionManager(
        ConnectionManager connectionManager,
        WindowManager windowManager)
    {
        var tcpClient = GetTcpClient(connectionManager);
        if (tcpClient == null)
            throw new InvalidOperationException("ConnectionManager has no active TCP connection");

        var sessionId = Guid.NewGuid().ToString();
        var sequenceManager = new SequenceManager();
        var logger = DefaultLoggerFactory.CreateLogger<Session>();

        var session = new Session(
            sessionId,
            tcpClient,
            DefaultCodec,
            sequenceManager,
            windowManager,
            logger);

        return session;
    }

    public static Session Create(
        string sessionId,
        TcpClient tcpClient,
        PduCodec codec,
        SequenceManager sequenceManager,
        WindowManager windowManager,
        ILogger<Session> logger)
    {
        return new Session(
            sessionId,
            tcpClient,
            codec,
            sequenceManager,
            windowManager,
            logger);
    }

    private static TcpClient? GetTcpClient(ConnectionManager connectionManager)
    {
        var field = typeof(ConnectionManager).GetField("_tcpClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(connectionManager) as TcpClient;
    }
}

public class SessionAccessor
{
    private readonly ConnectionManager _connectionManager;
    private readonly WindowManager _windowManager;
    private readonly SequenceManager _sequenceManager;

    public SessionAccessor(ConnectionManager connectionManager, WindowManager windowManager)
    {
        _connectionManager = connectionManager;
        _windowManager = windowManager;
        _sequenceManager = new SequenceManager();
    }

    public Session CreateSession()
    {
        var tcpClient = GetTcpClient();
        if (tcpClient == null)
            throw new InvalidOperationException("No active TCP connection");

        var session = new Session(
            Guid.NewGuid().ToString(),
            tcpClient,
            new PduCodec(),
            _sequenceManager,
            _windowManager,
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Session>());

        return session;
    }

    private TcpClient? GetTcpClient()
    {
        var field = typeof(ConnectionManager).GetField("_tcpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(_connectionManager) as TcpClient;
    }
}
