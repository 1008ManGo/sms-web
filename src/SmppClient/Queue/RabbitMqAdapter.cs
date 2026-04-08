using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SmppClient.Queue;

public class RabbitMqAdapter : IQueueAdapter
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly ILogger<RabbitMqAdapter> _logger;

    private IConnection? _connection;
    private IModel? _submitChannel;
    private IModel? _dlrChannel;
    private AsyncEventingBasicConsumer? _submitConsumer;
    private AsyncEventingBasicConsumer? _dlrConsumer;
    private readonly ConcurrentDictionary<ulong, ulong> _pendingTags = new();
    private ulong _submitTagCounter = 0;
    private ulong _dlrTagCounter = 0;
    private bool _disposed;

    public int SubmitQueueLength => GetQueueLength(_submitChannel, QueueNames.SubmitQueue);
    public int DlrQueueLength => GetQueueLength(_dlrChannel, QueueNames.DlrQueue);

    public RabbitMqAdapter(
        string host,
        int port,
        string username,
        string password,
        ILogger<RabbitMqAdapter> logger)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _host,
            Port = _port,
            UserName = _username,
            Password = _password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _submitChannel = _connection.CreateModel();
        _dlrChannel = _connection.CreateModel();

        _submitChannel.ExchangeDeclare(QueueNames.SubmitExchange, ExchangeType.Direct, durable: true);
        _submitChannel.QueueDeclare(QueueNames.SubmitQueue, durable: true, exclusive: false, autoDelete: false);
        _submitChannel.QueueBind(QueueNames.SubmitQueue, QueueNames.SubmitExchange, QueueNames.SubmitQueue);

        _dlrChannel.ExchangeDeclare(QueueNames.DlrExchange, ExchangeType.Fanout, durable: true);
        _dlrChannel.QueueDeclare(QueueNames.DlrQueue, durable: true, exclusive: false, autoDelete: false);
        _dlrChannel.QueueBind(QueueNames.DlrQueue, QueueNames.DlrExchange, "");

        _submitChannel.ExchangeDeclare(QueueNames.DeadLetterExchange, ExchangeType.Direct, durable: true);
        _submitChannel.QueueDeclare(
            QueueNames.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _submitChannel.BasicQos(0, 10, false);
        _dlrChannel.BasicQos(0, 10, false);

        _submitConsumer = new AsyncEventingBasicConsumer(_submitChannel);
        _dlrConsumer = new AsyncEventingBasicConsumer(_dlrChannel);

        _submitChannel.BasicConsume(QueueNames.SubmitQueue, false, _submitConsumer);
        _dlrChannel.BasicConsume(QueueNames.DlrQueue, false, _dlrConsumer);

        _logger.LogInformation("RabbitMQ initialized: {Host}:{Port}", _host, _port);
        return Task.CompletedTask;
    }

    public async Task PublishSubmitAsync(SmsMessage message)
    {
        if (_submitChannel == null)
            throw new InvalidOperationException("RabbitMQ not initialized");

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _submitChannel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Priority = (byte)message.Priority;
        properties.MessageId = message.Id;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _submitChannel.BasicPublish(
            exchange: QueueNames.SubmitExchange,
            routingKey: QueueNames.SubmitQueue,
            basicProperties: properties,
            body: body);

        await Task.CompletedTask;
        _logger.LogDebug("Published submit message: {MessageId}", message.Id);
    }

    public async Task PublishDlrAsync(DlrMessage message)
    {
        if (_dlrChannel == null)
            throw new InvalidOperationException("RabbitMQ not initialized");

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _dlrChannel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = message.MessageId;

        _dlrChannel.BasicPublish(
            exchange: QueueNames.DlrExchange,
            routingKey: "",
            basicProperties: properties,
            body: body);

        await Task.CompletedTask;
        _logger.LogDebug("Published DLR message: {MessageId}", message.MessageId);
    }

    public async Task<SmsMessage?> ConsumeSubmitAsync(CancellationToken cancellationToken)
    {
        if (_submitConsumer == null)
            return null;

        var tcs = new TaskCompletionSource<SmsMessage?>();
        cancellationToken.Register(() => tcs.TrySetCanceled());

        void Handler(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<SmsMessage>(body);
                var tag = Interlocked.Increment(ref _submitTagCounter);
                _pendingTags[tag] = ea.DeliveryTag;

                if (message != null)
                {
                    tcs.TrySetResult(message);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing submit message");
                tcs.TrySetResult(null);
            }
        }

        _submitConsumer.Received += Handler;

        try
        {
            var result = await tcs.Task;
            return result;
        }
        finally
        {
            _submitConsumer.Received -= Handler;
        }
    }

    public async Task<DlrMessage?> ConsumeDlrAsync(CancellationToken cancellationToken)
    {
        if (_dlrConsumer == null)
            return null;

        var tcs = new TaskCompletionSource<DlrMessage?>();
        cancellationToken.Register(() => tcs.TrySetCanceled());

        void Handler(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<DlrMessage>(body);
                var tag = Interlocked.Increment(ref _dlrTagCounter);
                _pendingTags[tag] = ea.DeliveryTag;

                if (message != null)
                {
                    tcs.TrySetResult(message);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing DLR message");
                tcs.TrySetResult(null);
            }
        }

        _dlrConsumer.Received += Handler;

        try
        {
            var result = await tcs.Task;
            return result;
        }
        finally
        {
            _dlrConsumer.Received -= Handler;
        }
    }

    public Task AckAsync(ulong deliveryTag)
    {
        _submitChannel?.BasicAck(deliveryTag, false);
        _dlrChannel?.BasicAck(deliveryTag, false);
        return Task.CompletedTask;
    }

    public Task NackAsync(ulong deliveryTag, bool requeue)
    {
        _submitChannel?.BasicNack(deliveryTag, false, requeue);
        _dlrChannel?.BasicNack(deliveryTag, false, requeue);
        return Task.CompletedTask;
    }

    private int GetQueueLength(IModel? channel, string queueName)
    {
        if (channel == null) return 0;

        try
        {
            var result = channel.QueueDeclarePassive(queueName);
            return (int)result.MessageCount;
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _submitChannel?.Close();
        _submitChannel?.Dispose();
        _dlrChannel?.Close();
        _dlrChannel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
