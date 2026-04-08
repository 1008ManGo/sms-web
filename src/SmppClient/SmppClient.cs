using Microsoft.Extensions.Logging;
using SmppClient.Connection;
using SmppClient.Protocol;

namespace SmppClient;

public class SmppClient : IDisposable
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<SmppClient> _logger;
    private bool _disposed;

    public SmppClient(ConnectionConfig config, ILogger<SmppClient> logger)
    {
        _connectionManager = new ConnectionManager(config, logger);
        _logger = logger;
        _connectionManager.ConnectionLost += OnConnectionLost;
    }

    public bool IsConnected => _connectionManager.IsConnected;
    public bool IsBound => _connectionManager.IsBound;

    public event EventHandler<Exception>? ConnectionLost;

    private void OnConnectionLost(object? sender, Exception ex)
    {
        ConnectionLost?.Invoke(this, ex);
    }

    public async Task ConnectAsync()
    {
        await _connectionManager.ConnectAsync();
    }

    public async Task ReconnectAsync()
    {
        await _connectionManager.ReconnectAsync();
    }

    public async Task<SubmitSmRespPdu> SubmitSmAsync(
        string sourceAddr,
        string destinationAddr,
        byte[] shortMessage,
        DataCoding dataCoding = DataCoding.GSM7Bit,
        RegisteredDelivery registeredDelivery = RegisteredDelivery.FinalDeliveryReceipt)
    {
        var request = new SubmitSmPdu
        {
            SourceAddr = sourceAddr,
            DestinationAddr = destinationAddr,
            ShortMessage = shortMessage,
            ShortMessageLength = (byte)shortMessage.Length,
            DataCoding = dataCoding,
            RegisteredDelivery = registeredDelivery,
            SourceAddrTon = Ton.International,
            SourceAddrNpi = Npi.E164,
            DestAddrTon = Ton.International,
            DestAddrNpi = Npi.E164
        };

        var response = await _connectionManager.SendRequestAsync(request, TimeSpan.FromSeconds(30));
        return (SubmitSmRespPdu)response;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connectionManager.Dispose();
    }
}
