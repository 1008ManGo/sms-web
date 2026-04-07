namespace SmppClient.Queue;

public static class QueueNames
{
    public const string SubmitExchange = "sms.submit";
    public const string SubmitQueue = "sms.submit";
    public const string DlrExchange = "sms.dlr";
    public const string DlrQueue = "sms.dlr";
    public const string DeadLetterExchange = "sms.dlx";
    public const string DeadLetterQueue = "sms.dlx";
}

public class SmsMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string LocalId { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public int Priority { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; } = 0;
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class DlrMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string LocalId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DlrTime { get; set; }
    public string? ErrorCode { get; set; }
}

public interface IQueueAdapter : IDisposable
{
    Task InitializeAsync();
    Task PublishSubmitAsync(SmsMessage message);
    Task PublishDlrAsync(DlrMessage message);
    Task<SmsMessage?> ConsumeSubmitAsync(CancellationToken cancellationToken);
    Task<DlrMessage?> ConsumeDlrAsync(CancellationToken cancellationToken);
    Task AckAsync(ulong deliveryTag);
    Task NackAsync(ulong deliveryTag, bool requeue);
    int SubmitQueueLength { get; }
    int DlrQueueLength { get; }
}
