using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using SmppClient.Core;
using SmppClient.Protocol;

namespace SmppClient.Connection;

public class Session : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly PduCodec _codec;
    private readonly SequenceManager _sequenceManager;
    private readonly WindowManager _windowManager;
    private readonly StateMachine _stateMachine;
    private readonly ILogger<Session> _logger;

    private NetworkStream? _stream;
    private readonly ConcurrentDictionary<uint, TaskCompletionSource<Pdu>> _pendingRequests = new();
    private CancellationTokenSource? _receiveCts;
    private bool _disposed;

    public string SessionId { get; }
    public StateMachine StateMachine => _stateMachine;
    public WindowManager WindowManager => _windowManager;
    public bool IsConnected => _tcpClient.Connected;
    public bool IsBound => _stateMachine.IsBound;

    public event EventHandler<Pdu>? PduReceived;
    public event EventHandler<Exception>? ConnectionLost;

    public Session(
        string sessionId,
        TcpClient tcpClient,
        PduCodec codec,
        SequenceManager sequenceManager,
        WindowManager windowManager,
        ILogger<Session> logger)
    {
        SessionId = sessionId;
        _tcpClient = tcpClient;
        _codec = codec;
        _sequenceManager = sequenceManager;
        _windowManager = windowManager;
        _stateMachine = new StateMachine(logger as ILogger<StateMachine> ?? 
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StateMachine>());
        _logger = logger;
    }

    public async Task StartAsync()
    {
        _stream = _tcpClient.GetStream();
        _receiveCts = new CancellationTokenSource();
        _ = ReceiveLoopAsync(_receiveCts.Token);
        await Task.CompletedTask;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        byte[]? headerBuffer = null;
        byte[]? bodyBuffer = null;
        byte[]? pduData = null;

        try
        {
            headerBuffer = ArrayPool<byte>.Shared.Rent(16);
            bodyBuffer = ArrayPool<byte>.Shared.Rent(65520);

            while (!cancellationToken.IsCancellationRequested && _stream != null)
            {
                int totalRead = 0;

                while (totalRead < 16)
                {
                    var bytesRead = await _stream.ReadAsync(
                        bodyBuffer.AsMemory(totalRead, 16 - totalRead), 
                        cancellationToken);
                    if (bytesRead == 0)
                    {
                        HandleConnectionLost();
                        return;
                    }
                    totalRead += bytesRead;
                }

                var commandLength = BitConverter.ToUInt32(bodyBuffer, 0);
                if (commandLength > 65536 || commandLength < 16)
                {
                    _logger.LogWarning("Invalid command length: {Length}", commandLength);
                    continue;
                }

                var bodyLength = (int)commandLength - 16;
                if (bodyLength > 0)
                {
                    var offset = 16;
                    while (offset < commandLength)
                    {
                        var bytesToRead = Math.Min(bodyLength - (offset - 16), 65520);
                        var bytesRead = await _stream.ReadAsync(
                            bodyBuffer.AsMemory(offset, bytesToRead), 
                            cancellationToken);
                        if (bytesRead == 0)
                        {
                            HandleConnectionLost();
                            return;
                        }
                        offset += bytesRead;
                    }
                }

                pduData = ArrayPool<byte>.Shared.Rent((int)commandLength);
                Buffer.BlockCopy(bodyBuffer, 0, pduData, 0, (int)commandLength);

                var pdu = _codec.Decode(pduData);
                await HandlePduAsync(pdu);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Receive loop error");
            HandleConnectionLost();
        }
        finally
        {
            if (headerBuffer != null)
                ArrayPool<byte>.Shared.Return(headerBuffer);
            if (bodyBuffer != null)
                ArrayPool<byte>.Shared.Return(bodyBuffer);
            if (pduData != null)
                ArrayPool<byte>.Shared.Return(pduData);
        }
    }

    private async Task HandlePduAsync(Pdu pdu)
    {
        _logger.LogDebug("Received PDU: {CommandId}, seq: {Sequence}", pdu.CommandId, pdu.SequenceNumber);

        if (pdu.CommandId == CommandId.EnquireLinkResp)
        {
            CompletePendingRequest(pdu);
            return;
        }

        if (pdu.CommandId == CommandId.DeliverSm || pdu.CommandId == CommandId.DataSm)
        {
            PduReceived?.Invoke(this, pdu);
            return;
        }

        if (IsResponsePdu(pdu.CommandId))
        {
            CompletePendingRequest(pdu);
        }
        else
        {
            PduReceived?.Invoke(this, pdu);
        }

        await Task.CompletedTask;
    }

    private bool IsResponsePdu(CommandId commandId)
    {
        return commandId switch
        {
            CommandId.BindTransmitterResp or CommandId.BindReceiverResp or 
            CommandId.BindTransceiverResp or CommandId.SubmitSmResp or 
            CommandId.DeliverSmResp or CommandId.UnbindResp or 
            CommandId.EnquireLinkResp => true,
            _ => false
        };
    }

    private void CompletePendingRequest(Pdu pdu)
    {
        if (_pendingRequests.TryRemove(pdu.SequenceNumber, out var tcs))
        {
            _windowManager.Release();
            tcs.SetResult(pdu);
        }
    }

    private void HandleConnectionLost()
    {
        _logger.LogWarning("Connection lost for session {SessionId}", SessionId);
        _stateMachine.TryTransition(SessionStateEvent.ConnectionLost);
        ConnectionLost?.Invoke(this, new Exception("Connection lost"));
    }

    public async Task<Pdu> SendRequestAsync(Pdu request, TimeSpan timeout)
    {
        if (!_stateMachine.CanSend)
            throw new InvalidOperationException($"Cannot send in state {_stateMachine.CurrentState}");

        if (!_windowManager.TryAcquire())
            throw new InvalidOperationException("Window is full");

        request.SequenceNumber = _sequenceManager.Next();

        var tcs = new TaskCompletionSource<Pdu>();
        _pendingRequests[request.SequenceNumber] = tcs;

        try
        {
            var data = _codec.Encode(request);
            await _stream!.WriteAsync(data);

            _logger.LogDebug("Sent PDU: {CommandId}, seq: {Sequence}", request.CommandId, request.SequenceNumber);

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _receiveCts!.Token);

            try
            {
                return await tcs.Task.WaitAsync(linkedCts.Token);
            }
            catch (TimeoutException)
            {
                _pendingRequests.TryRemove(request.SequenceNumber, out _);
                _windowManager.Release();
                throw;
            }
        }
        catch
        {
            _pendingRequests.TryRemove(request.SequenceNumber, out _);
            _windowManager.Release();
            throw;
        }
    }

    public async Task SendResponseAsync(Pdu response)
    {
        var data = _codec.Encode(response);
        await _stream!.WriteAsync(data);
    }

    public async Task BindTransceiverAsync(string systemId, string password, string systemType)
    {
        _stateMachine.TryTransition(SessionStateEvent.Connect);

        var request = new BindTransceiverPdu
        {
            SystemId = systemId,
            Password = password,
            SystemType = systemType,
            InterfaceVersion = Ton.Unknown,
            AddressTon = Ton.Unknown,
            AddressNpi = Npi.Unknown
        };

        _stateMachine.TryTransition(SessionStateEvent.ConnectSuccess);
        _stateMachine.TryTransition(SessionStateEvent.BindSuccess);

        var response = await SendRequestAsync(request, TimeSpan.FromSeconds(30));
        if (response.Status != CommandStatus.ESME_ROK)
            throw new Exception($"Bind failed: {response.Status}");
    }

    public async Task UnbindAsync()
    {
        if (!_stateMachine.TryTransition(SessionStateEvent.Unbind))
            return;

        var request = new UnbindPdu { SequenceNumber = _sequenceManager.Next() };
        var data = _codec.Encode(request);
        await _stream!.WriteAsync(data);

        await Task.Delay(100);
        _stateMachine.TryTransition(SessionStateEvent.UnbindComplete);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _stream?.Dispose();
        _tcpClient.Dispose();

        foreach (var tcs in _pendingRequests.Values)
            tcs.SetCanceled();
        _pendingRequests.Clear();
    }
}
