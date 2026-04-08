using SmppClient.Protocol;
using SmppClient.Queue;
using SmppClient.Routing;

namespace SmppClient.Services;

public class SubmitRequest
{
    public Guid? UserId { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public List<string>? AllowedAccountIds { get; set; }
    public int Priority { get; set; } = 5;
    public string? Ext { get; set; }
    public DataCoding? PreferredCoding { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class SubmitResult
{
    public bool Success { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string LocalId { get; set; } = string.Empty;
    public int SegmentCount { get; set; } = 1;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IdempotencyKey { get; set; }
    public bool IsDuplicate { get; set; }
}

public class SubmitService : IDisposable
{
    private readonly RouteStrategy _routeStrategy;
    private readonly LongMessageProcessor _longMessageProcessor;
    private readonly DlrProcessor _dlrProcessor;
    private readonly ILogger<SubmitService> _logger;
    private readonly IQueueAdapter? _queueAdapter;
    private readonly CancellationTokenSource _cts = new();
    private Task? _consumerTask;
    private bool _disposed;

    public event EventHandler<SubmitResult>? SubmitCompleted;

    public SubmitService(
        RouteStrategy routeStrategy,
        LongMessageProcessor longMessageProcessor,
        DlrProcessor dlrProcessor,
        ILogger<SubmitService> logger,
        IQueueAdapter? queueAdapter = null)
    {
        _routeStrategy = routeStrategy;
        _longMessageProcessor = longMessageProcessor;
        _dlrProcessor = dlrProcessor;
        _logger = logger;
        _queueAdapter = queueAdapter;

        _dlrProcessor.DlrReceived += OnDlrReceived;
    }

    public async Task<SubmitResult> SubmitAsync(SubmitRequest request)
    {
        var localId = Guid.NewGuid().ToString("N")[12];

        try
        {
            Session? session;
            if (request.AllowedAccountIds != null && request.AllowedAccountIds.Any())
            {
                session = _routeStrategy.GetSession(request.AllowedAccountIds);
            }
            else if (!string.IsNullOrEmpty(request.AccountId))
            {
                session = _routeStrategy.GetSession(request.AccountId);
            }
            else
            {
                session = _routeStrategy.GetBestSession();
            }

            if (session == null)
            {
                return new SubmitResult
                {
                    Success = false,
                    LocalId = localId,
                    ErrorCode = "NO_SESSION",
                    ErrorMessage = "No available session"
                };
            }

            var splitResult = _longMessageProcessor.Split(request.Content, request.PreferredCoding);
            var results = new List<(bool Success, string MessageId, string? ErrorCode)>();

            foreach (var segment in splitResult.Segments)
            {
                var segmentResult = await SendSegmentAsync(session, request, segment, localId);
                results.Add(segmentResult);

                if (!segmentResult.Success)
                    break;

                if (splitResult.Segments.Count > 1)
                    await Task.Delay(50);
            }

            var firstSuccess = results.FirstOrDefault(r => r.Success);
            var lastResult = results.LastOrDefault();

            if (firstSuccess.Success)
            {
                _dlrProcessor.Register(localId, firstSuccess.MessageId, request.Mobile, request.Content);

                return new SubmitResult
                {
                    Success = true,
                    MessageId = firstSuccess.MessageId,
                    LocalId = localId,
                    SegmentCount = splitResult.Segments.Count
                };
            }

            return new SubmitResult
            {
                Success = false,
                LocalId = localId,
                SegmentCount = splitResult.Segments.Count,
                ErrorCode = lastResult.ErrorCode ?? "SEND_FAILED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Submit failed for {Mobile}", request.Mobile);
            return new SubmitResult
            {
                Success = false,
                LocalId = localId,
                ErrorCode = "EXCEPTION",
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<(bool Success, string MessageId, string? ErrorCode)> SendSegmentAsync(
        Connection.Session session,
        SubmitRequest request,
        LongMessageProcessor.Segment segment,
        string localId)
    {
        var submitSm = new SubmitSmPdu
        {
            SourceAddr = request.Ext ?? "106XXXX",
            DestinationAddr = NormalizeMobile(request.Mobile),
            DataCoding = segment.Coding,
            RegisteredDelivery = RegisteredDelivery.FinalDeliveryReceipt,
            SourceAddrTon = Ton.International,
            SourceAddrNpi = Npi.E164,
            DestAddrTon = Ton.International,
            DestAddrNpi = Npi.E164,
            ShortMessageLength = (byte)segment.Data.Length,
            ShortMessage = segment.Data
        };

        if (segment.TotalSegments > 1)
        {
            submitSm.OptionalParameters.Add(new Tlv(TlvTag.SarMsgRefNum, new[] { (byte)segment.ReferenceNumber }));
            submitSm.OptionalParameters.Add(new Tlv(TlvTag.SarTotalSegments, new[] { (byte)segment.TotalSegments }));
            submitSm.OptionalParameters.Add(new Tlv(TlvTag.SarSegmentSeqnum, new[] { (byte)segment.SegmentNumber }));
        }

        if (segment.UsedPayload)
        {
            submitSm.OptionalParameters.Add(new Tlv(TlvTag.MessagePayload, segment.Data));
            submitSm.ShortMessageLength = 0;
            submitSm.ShortMessage = Array.Empty<byte>();
        }

        try
        {
            var response = await session.SendRequestAsync(submitSm, TimeSpan.FromSeconds(30));

            if (response.Status != CommandStatus.ESME_ROK)
            {
                return (false, string.Empty, $"ESME_{response.Status:X}");
            }

            var submitResp = (SubmitSmRespPdu)response;
            return (true, submitResp.MessageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Send segment failed");
            return (false, string.Empty, "SEND_FAILED");
        }
    }

    private void OnDlrReceived(object? sender, DlrCallback callback)
    {
        var result = new SubmitResult
        {
            Success = callback.Status == DlrStatus.Delivered,
            MessageId = callback.MessageId,
            LocalId = callback.LocalId,
            ErrorCode = callback.ErrorCode
        };

        SubmitCompleted?.Invoke(this, result);
    }

    public async Task StartQueueConsumerAsync()
    {
        if (_queueAdapter == null)
            return;

        _consumerTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var message = await _queueAdapter.ConsumeSubmitAsync(_cts.Token);
                    if (message != null)
                    {
                        var request = new SubmitRequest
                        {
                            Mobile = message.Mobile,
                            Content = message.Content,
                            AccountId = message.AccountId,
                            Priority = message.Priority
                        };

                        var result = await SubmitAsync(request);
                        _queueAdapter.AckAsync(0);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in queue consumer");
                    await Task.Delay(1000, _cts.Token);
                }
            }
        }, _cts.Token);
    }

    public void StopQueueConsumer()
    {
        _cts.Cancel();
        _consumerTask?.Wait(TimeSpan.FromSeconds(5));
    }

    private string NormalizeMobile(string mobile)
    {
        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");
        if (mobile.StartsWith("+"))
            return mobile[1..];
        if (mobile.StartsWith("86"))
            return mobile;
        return "86" + mobile;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _cts.Dispose();
        _consumerTask?.Dispose();
    }
}
