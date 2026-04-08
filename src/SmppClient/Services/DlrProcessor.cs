using Microsoft.Extensions.Logging;
using SmppClient.Protocol;

namespace SmppClient.Services;

public enum DlrStatus
{
    Pending,
    Delivered,
    Failed,
    Expired,
    Rejected,
    Unknown
}

public class DlrRecord
{
    public string MessageId { get; set; } = string.Empty;
    public string LocalId { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DlrStatus Status { get; set; } = DlrStatus.Pending;
    public DateTime SubmitTime { get; set; }
    public DateTime? DlrTime { get; set; }
    public string? ErrorCode { get; set; }
    public string? NetworkErrorCode { get; set; }
    public byte? MessageState { get; set; }
}

public class DlrCallback
{
    public string MessageId { get; set; } = string.Empty;
    public string LocalId { get; set; } = string.Empty;
    public DlrStatus Status { get; set; }
    public DateTime DlrTime { get; set; }
    public string? ErrorCode { get; set; }
}

public class DlrProcessor
{
    private readonly Dictionary<string, DlrRecord> _pendingDlrs = new();
    private readonly object _lock = new();
    private readonly ILogger<DlrProcessor> _logger;
    private readonly TimeSpan _dlvTimeout;
    private readonly Timer _timeoutTimer;

    public event EventHandler<DlrCallback>? DlrReceived;

    public DlrProcessor(ILogger<DlrProcessor> logger, TimeSpan? dlrTimeout = null)
    {
        _logger = logger;
        _dlvTimeout = dlrTimeout ?? TimeSpan.FromHours(24);
        _timeoutTimer = new Timer(CheckTimeout, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public void Register(string localId, string messageId, string mobile, string content)
    {
        lock (_lock)
        {
            _pendingDlrs[messageId] = new DlrRecord
            {
                LocalId = localId,
                MessageId = messageId,
                Mobile = mobile,
                Content = content,
                Status = DlrStatus.Pending,
                SubmitTime = DateTime.UtcNow
            };
            _logger.LogDebug("Registered DLR: {MessageId} -> {LocalId}", messageId, localId);
        }
    }

    public void ProcessDeliverSm(DeliverSmPdu pdu, byte? messageState = null)
    {
        var receiptedMessageId = GetTlvString(pdu, TlvTag.ReceiptedMessageId);
        var messageId = !string.IsNullOrEmpty(receiptedMessageId) ? receiptedMessageId : ExtractMessageId(pdu.ShortMessage);
        var state = messageState ?? GetTlvByte(pdu, TlvTag.MessageState);
        var networkError = GetTlvString(pdu, TlvTag.NetworkErrorCode);

        if (string.IsNullOrEmpty(messageId))
        {
            _logger.LogWarning("DLR received without message_id");
            return;
        }

        DlrRecord? record;
        lock (_lock)
        {
            _pendingDlrs.TryGetValue(messageId, out record);
        }

        if (record == null)
        {
            _logger.LogWarning("DLR for unknown message_id: {MessageId}", messageId);
            return;
        }

        var status = MapMessageState(state);
        record.Status = status;
        record.DlrTime = DateTime.UtcNow;
        record.MessageState = state;
        record.NetworkErrorCode = networkError;

        var callback = new DlrCallback
        {
            MessageId = record.MessageId,
            LocalId = record.LocalId,
            Status = status,
            DlrTime = record.DlrTime.Value,
            ErrorCode = networkError
        };

        _logger.LogInformation(
            "DLR received: {MessageId} -> {Status}, delay: {Delay}s",
            messageId, status, (record.DlrTime.Value - record.SubmitTime).TotalSeconds);

        DlrReceived?.Invoke(this, callback);
    }

    public void ProcessTextDlr(string text)
    {
        var parts = text.Split(' ');
        string? messageId = null;
        DlrStatus status = DlrStatus.Unknown;
        string? errorCode = null;

        foreach (var part in parts)
        {
            var kv = part.Split(':');
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLower();
            var value = kv[1].Trim();

            switch (key)
            {
                case "id":
                    messageId = value;
                    break;
                case "stat":
                    status = ParseStatus(value);
                    break;
                case "err":
                    errorCode = value;
                    break;
            }
        }

        if (string.IsNullOrEmpty(messageId))
        {
            _logger.LogWarning("Text DLR without id: {Text}", text);
            return;
        }

        DlrRecord? record;
        lock (_lock)
        {
            _pendingDlrs.TryGetValue(messageId, out record);
        }

        if (record == null)
        {
            _logger.LogWarning("Text DLR for unknown message_id: {MessageId}", messageId);
            return;
        }

        record.Status = status;
        record.DlrTime = DateTime.UtcNow;
        record.ErrorCode = errorCode;

        var callback = new DlrCallback
        {
            MessageId = record.MessageId,
            LocalId = record.LocalId,
            Status = status,
            DlrTime = record.DlrTime.Value,
            ErrorCode = errorCode
        };

        _logger.LogInformation("Text DLR received: {MessageId} -> {Status}", messageId, status);
        DlrReceived?.Invoke(this, callback);
    }

    public DlrStatus QueryStatus(string messageId)
    {
        lock (_lock)
        {
            if (_pendingDlrs.TryGetValue(messageId, out var record))
                return record.Status;
        }
        return DlrStatus.Unknown;
    }

    public DlrRecord? GetRecord(string messageId)
    {
        lock (_lock)
        {
            _pendingDlrs.TryGetValue(messageId, out var record);
            return record;
        }
    }

    public void Remove(string messageId)
    {
        lock (_lock)
        {
            _pendingDlrs.Remove(messageId);
        }
    }

    private void CheckTimeout(object? state)
    {
        var timeoutRecords = new List<string>();
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            foreach (var kvp in _pendingDlrs)
            {
                if (kvp.Value.Status == DlrStatus.Pending &&
                    now - kvp.Value.SubmitTime > _dlvTimeout)
                {
                    timeoutRecords.Add(kvp.Key);
                }
            }

            foreach (var messageId in timeoutRecords)
            {
                if (_pendingDlrs.TryGetValue(messageId, out var record))
                {
                    record.Status = DlrStatus.Unknown;
                    record.DlrTime = now;

                    var callback = new DlrCallback
                    {
                        MessageId = record.MessageId,
                        LocalId = record.LocalId,
                        Status = DlrStatus.Unknown,
                        DlrTime = now
                    };

                    _logger.LogWarning("DLR timeout: {MessageId}", messageId);
                    DlrReceived?.Invoke(this, callback);
                }
            }

            foreach (var messageId in timeoutRecords)
            {
                _pendingDlrs.Remove(messageId);
            }
        }
    }

    private DlrStatus MapMessageState(byte? state)
    {
        if (!state.HasValue) return DlrStatus.Unknown;

        return state.Value switch
        {
            0x00 => DlrStatus.Pending,
            0x01 => DlrStatus.Delivered,
            0x02 => DlrStatus.Expired,
            0x03 => DlrStatus.Rejected,
            0x04 => DlrStatus.Unknown,
            0x05 => DlrStatus.Rejected,
            0x06 => DlrStatus.Failed,
            0x07 => DlrStatus.Pending,
            0x08 => DlrStatus.Pending,
            0x09 => DlrStatus.Rejected,
            0x0A => DlrStatus.Pending,
            0x0B => DlrStatus.Pending,
            0x0C => DlrStatus.Expired,
            _ => DlrStatus.Unknown
        };
    }

    private DlrStatus ParseStatus(string status)
    {
        return status.ToUpper() switch
        {
            "DELIVRD" => DlrStatus.Delivered,
            "EXPIRED" => DlrStatus.Expired,
            "REJECTD" => DlrStatus.Rejected,
            "UNDELIV" => DlrStatus.Failed,
            "FAILED" => DlrStatus.Failed,
            "PENDING" => DlrStatus.Pending,
            "UNKNOWN" => DlrStatus.Unknown,
            _ => DlrStatus.Unknown
        };
    }

    private string? GetTlvString(Pdu pdu, TlvTag tag)
    {
        if (pdu.OptionalParameters.TryGet(tag, out var tlv))
            return tlv.GetString();
        return null;
    }

    private byte? GetTlvByte(Pdu pdu, TlvTag tag)
    {
        if (pdu.OptionalParameters.TryGet(tag, out var tlv))
            return tlv.GetByte();
        return null;
    }

    private string? ExtractMessageId(byte[]? shortMessage)
    {
        if (shortMessage == null || shortMessage.Length == 0)
            return null;

        var text = System.Text.Encoding.ASCII.GetString(shortMessage);
        var parts = text.Split(':');
        if (parts.Length >= 2 && parts[0].Trim().ToLower() == "id")
            return parts[1].Trim();
        return text.Trim();
    }

    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _pendingDlrs.Count(kvp => kvp.Value.Status == DlrStatus.Pending);
            }
        }
    }

    public void Dispose()
    {
        _timeoutTimer.Dispose();
    }
}
