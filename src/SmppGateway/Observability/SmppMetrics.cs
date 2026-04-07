using Prometheus;

namespace SmppGateway.Observability;

public class SmppMetrics
{
    public static readonly Counter ConnectedSessions = Prometheus.Metrics
        .CreateGauge("smpp_connected_sessions", "Number of connected SMPP sessions");

    public static readonly Counter ReconnectTotal = Prometheus.Metrics
        .CreateCounter("smpp_reconnect_total", "Total number of reconnection attempts");

    public static readonly Gauge SubmitTps = Prometheus.Metrics
        .CreateGauge("smpp_submit_tps", "Current SMS submit TPS");

    public static readonly Counter SubmitSuccessTotal = Prometheus.Metrics
        .CreateCounter("smpp_submit_success_total", "Total number of successful SMS submissions");

    public static readonly Counter SubmitFailTotal = Prometheus.Metrics
        .CreateCounter("smpp_submit_fail_total", "Total number of failed SMS submissions");

    public static readonly Counter DlrReceivedTotal = Prometheus.Metrics
        .CreateCounter("smpp_dlr_received_total", "Total number of DLRs received");

    public static readonly Histogram DlrDelaySeconds = Prometheus.Metrics
        .CreateHistogram("smpp_dlr_delay_seconds", "DLR delay in seconds",
            new HistogramConfiguration
            {
                Buckets = new[] { 0.1, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0, 120.0, 300.0 }
            });

    public static readonly Gauge WindowUsage = Prometheus.Metrics
        .CreateGauge("smpp_window_usage", "Window usage percentage");

    public static readonly Gauge QueueLength = Prometheus.Metrics
        .CreateGauge("smpp_queue_length", "Current queue length");

    public static readonly Gauge CircuitBreakerState = Prometheus.Metrics
        .CreateGauge("smpp_circuit_breaker_state", "Circuit breaker state (0=closed, 1=open, 2=half-open)");

    public static readonly Counter ApiRequestTotal = Prometheus.Metrics
        .CreateCounter("smpp_api_request_total", "Total API requests",
            new CounterConfiguration { LabelNames = new[] { "method", "endpoint", "status" } });

    public static readonly Histogram ApiRequestDuration = Prometheus.Metrics
        .CreateHistogram("smpp_api_request_duration_seconds", "API request duration",
            new HistogramConfiguration
            {
                Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0 },
                LabelNames = new[] { "method", "endpoint" }
            });

    public static readonly Gauge UserBalance = Prometheus.Metrics
        .CreateGauge("smpp_user_balance", "User balance",
            new GaugeConfiguration { LabelNames = new[] { "user_id" } });

    public static readonly Counter BillingDeductionTotal = Prometheus.Metrics
        .CreateCounter("smpp_billing_deduction_total", "Total billing deductions",
            new CounterConfiguration { LabelNames = new[] { "user_id" } });

    public static readonly Counter BillingChargeTotal = Prometheus.Metrics
        .CreateCounter("smpp_billing_charge_total", "Total billing charges",
            new CounterConfiguration { LabelNames = new[] { "country_code" } });
}

public class MetricsCollector
{
    private readonly Timer _collectionTimer;
    private readonly ISmppClientManager _smppClientManager;

    public MetricsCollector(ISmppClientManager smppClientManager)
    {
        _smppClientManager = smppClientManager;
        _collectionTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void CollectMetrics(object? state)
    {
        try
        {
            SmppMetrics.ConnectedSessions.Set(_smppClientManager.HealthySessions);

            var routeStrategy = _smppClientManager.GetRouteStrategy();
            foreach (var pool in routeStrategy.GetAllPools())
            {
                var healthySessions = pool.SessionCount;
                var totalSessions = pool.TotalSessionCount;

                if (healthySessions > 0)
                {
                    var avgWindowUsage = pool.GetHealthySessions()
                        .Select(s => s.WindowManager.UsagePercentage)
                        .DefaultIfEmpty(0)
                        .Average();

                    SmppMetrics.WindowUsage.Set(avgWindowUsage);
                }
            }

            var queueAdapter = GetQueueAdapter();
            if (queueAdapter != null)
            {
                SmppMetrics.QueueLength.Set(queueAdapter.SubmitQueueLength);
            }
        }
        catch
        {
        }
    }

    private IQueueAdapter? GetQueueAdapter()
    {
        var field = _smppClientManager.GetType().GetField("_queueAdapter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(_smppClientManager) as IQueueAdapter;
    }

    public void RecordSubmitSuccess(string accountId)
    {
        SmppMetrics.SubmitSuccessTotal.Inc();
    }

    public void RecordSubmitFail(string accountId, string errorCode)
    {
        SmppMetrics.SubmitFailTotal.Inc();
    }

    public void RecordDlrReceived(double delaySeconds)
    {
        SmppMetrics.DlrReceivedTotal.Inc();
        SmppMetrics.DlrDelaySeconds.Observe(delaySeconds);
    }

    public void RecordReconnect()
    {
        SmppMetrics.ReconnectTotal.Inc();
    }

    public void RecordCircuitBreakerState(string name, CircuitBreakerState state)
    {
        SmppMetrics.CircuitBreakerState.WithLabels(name).Set((double)state);
    }

    public void RecordApiRequest(string method, string endpoint, int status, double durationSeconds)
    {
        SmppMetrics.ApiRequestTotal.WithLabels(method, endpoint, status.ToString()).Inc();
        SmppMetrics.ApiRequestDuration.WithLabels(method, endpoint).Observe(durationSeconds);
    }

    public void RecordBillingDeduction(string userId, decimal amount)
    {
        SmppMetrics.BillingDeductionTotal.WithLabels(userId).Inc(amount);
    }

    public void RecordBillingCharge(string countryCode)
    {
        SmppMetrics.BillingChargeTotal.WithLabels(countryCode).Inc();
    }

    public void Dispose()
    {
        _collectionTimer.Dispose();
    }
}

public interface ISmppClientManager
{
    Task StartAsync();
    Task StopAsync();
    object GetSubmitService();
    object GetDlrProcessor();
    object GetRouteStrategy();
    int TotalSessions { get; }
    int HealthySessions { get; }
}

public enum CircuitBreakerState
{
    Closed = 0,
    Open = 1,
    HalfOpen = 2
}

public interface IQueueAdapter
{
    Task InitializeAsync();
    int SubmitQueueLength { get; }
    int DlrQueueLength { get; }
}
