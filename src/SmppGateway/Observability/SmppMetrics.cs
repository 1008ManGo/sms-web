using Prometheus;

namespace SmppGateway.Observability;

public class SmppMetrics
{
    public static readonly Gauge ConnectedSessions = Prometheus.Metrics
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

    public static readonly Gauge ChannelAvailability = Prometheus.Metrics
        .CreateGauge("smpp_channel_availability", "Channel availability percentage",
            new GaugeConfiguration { LabelNames = new[] { "account_id" } });

    public static readonly Gauge SystemAvailability = Prometheus.Metrics
        .CreateGauge("smpp_system_availability", "System-wide availability percentage");

    public static readonly Counter SubmitLatencyTotal = Prometheus.Metrics
        .CreateCounter("smpp_submit_latency_total", "Submit latency observations");

    public static readonly Histogram SubmitLatencySeconds = Prometheus.Metrics
        .CreateHistogram("smpp_submit_latency_seconds", "SMS submit latency in seconds",
            new HistogramConfiguration
            {
                Buckets = new[] { 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 },
                LabelNames = new[] { "account_id" }
            });

    public static readonly Gauge SubmitSuccessRate = Prometheus.Metrics
        .CreateGauge("smpp_submit_success_rate", "Submit success rate (last 5 min)",
            new GaugeConfiguration { LabelNames = new[] { "account_id" } });

    public static readonly Gauge SystemSubmitSuccessRate = Prometheus.Metrics
        .CreateGauge("smpp_system_submit_success_rate", "System-wide submit success rate");

    public static readonly Counter AlertTotal = Prometheus.Metrics
        .CreateCounter("smpp_alert_total", "Total alerts generated",
            new CounterConfiguration { LabelNames = new[] { "type", "severity" } });
}

public class MetricsCollector
{
    private readonly Timer _collectionTimer;
    private readonly ISmppClientManager _smppClientManager;
    private readonly SlaTracker _slaTracker;
    private readonly Dictionary<string, DateTime> _channelLastHeartbeat = new();
    private readonly object _lock = new();

    public MetricsCollector(ISmppClientManager smppClientManager)
    {
        _smppClientManager = smppClientManager;
        _slaTracker = new SlaTracker();
        _collectionTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    public SlaTracker SlaTracker => _slaTracker;

    private void CollectMetrics(object? state)
    {
        try
        {
            SmppMetrics.ConnectedSessions.Set(_smppClientManager.HealthySessions);

            var routeStrategy = _smppClientManager.GetRouteStrategy();
            var allPools = routeStrategy.GetAllPools().ToList();
            var totalSessions = 0;
            var healthySessions = 0;

            foreach (var pool in allPools)
            {
                totalSessions += pool.TotalSessionCount;
                healthySessions += pool.SessionCount;

                if (pool.SessionCount > 0)
                {
                    var avgWindowUsage = pool.GetHealthySessions()
                        .Select(s => s.WindowManager.UsagePercentage)
                        .DefaultIfEmpty(0)
                        .Average();

                    SmppMetrics.WindowUsage.WithLabels(pool.AccountId).Set(avgWindowUsage);

                    var availability = pool.SessionCount > 0 ? 100.0 : 0.0;
                    SmppMetrics.ChannelAvailability.WithLabels(pool.AccountId).Set(availability);

                    lock (_lock)
                    {
                        _channelLastHeartbeat[pool.AccountId] = DateTime.UtcNow;
                    }

                    var (successRate, p99, p95, p90) = _slaTracker.GetChannelSla(pool.AccountId);
                    SmppMetrics.SubmitSuccessRate.WithLabels(pool.AccountId).Set(successRate);
                }
                else
                {
                    SmppMetrics.ChannelAvailability.WithLabels(pool.AccountId).Set(0);
                }
            }

            lock (_lock)
            {
                foreach (var channelId in _channelLastHeartbeat.Keys.ToList())
                {
                    if (!allPools.Any(p => p.AccountId == channelId))
                    {
                        _channelLastHeartbeat.Remove(channelId);
                    }
                }
            }

            var (systemSuccessRate, systemP99) = _slaTracker.GetSystemSla();
            SmppMetrics.SystemSubmitSuccessRate.Set(systemSuccessRate);

            var systemAvailability = allPools.Count > 0
                ? (healthySessions / (double)Math.Max(totalSessions, 1)) * 100
                : 0;
            SmppMetrics.SystemAvailability.Set(systemAvailability);

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

    public void RecordSubmitSuccess(string accountId, double latencySeconds)
    {
        SmppMetrics.SubmitSuccessTotal.Inc();
        SmppMetrics.SubmitLatencySeconds.WithLabels(accountId).Observe(latencySeconds);
        _slaTracker.RecordSubmit(accountId, true, latencySeconds);
    }

    public void RecordSubmitFail(string accountId, string errorCode, double latencySeconds)
    {
        SmppMetrics.SubmitFailTotal.Inc();
        _slaTracker.RecordSubmit(accountId, false, latencySeconds);
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

public class SlaTracker
{
    private readonly Dictionary<string, ChannelSlaData> _channelData = new();
    private readonly object _lock = new();
    private readonly TimeSpan _windowSize = TimeSpan.FromMinutes(5);

    public void RecordSubmit(string accountId, bool success, double latencySeconds)
    {
        lock (_lock)
        {
            if (!_channelData.TryGetValue(accountId, out var data))
            {
                data = new ChannelSlaData();
                _channelData[accountId] = data;
            }

            data.RecordSubmit(success, latencySeconds);
            CleanupOldData(data);
        }
    }

    public void RecordAlert(string accountId, string alertType, string severity)
    {
        SmppMetrics.AlertTotal.WithLabels(alertType, severity).Inc();
    }

    public (double SuccessRate, double P99Latency, double P95Latency, double P90Latency) GetChannelSla(string accountId)
    {
        lock (_lock)
        {
            if (!_channelData.TryGetValue(accountId, out var data))
                return (100.0, 0, 0, 0);

            return data.GetMetrics();
        }
    }

    public (double SuccessRate, double P99Latency) GetSystemSla()
    {
        lock (_lock)
        {
            if (_channelData.Count == 0)
                return (100.0, 0);

            var totalSuccess = 0.0;
            var totalCount = 0;
            var allLatencies = new List<double>();

            foreach (var data in _channelData.Values)
            {
                totalSuccess += data.TotalSuccess;
                totalCount += data.TotalCount;
                allLatencies.AddRange(data.GetRecentLatencies());
            }

            if (totalCount == 0)
                return (100.0, 0);

            allLatencies.Sort();
            var successRate = (totalSuccess / totalCount) * 100;
            var p99Index = (int)(allLatencies.Count * 0.99);
            var p99Latency = p99Index < allLatencies.Count ? allLatencies[p99Index] : 0;

            return (successRate, p99Latency);
        }
    }

    private void CleanupOldData(ChannelSlaData data)
    {
        var cutoff = DateTime.UtcNow - _windowSize;
        data.RemoveOldEntries(cutoff);
    }

    private class ChannelSlaData
    {
        private readonly List<(DateTime Time, bool Success, double Latency)> _records = new();
        private readonly object _lock = new();

        public double TotalSuccess => _records.Count(r => r.Success);
        public int TotalCount => _records.Count;

        public void RecordSubmit(bool success, double latencySeconds)
        {
            lock (_lock)
            {
                _records.Add((DateTime.UtcNow, success, latencySeconds));
            }
        }

        public List<double> GetRecentLatencies()
        {
            lock (_lock)
            {
                return _records.Select(r => r.Latency).ToList();
            }
        }

        public (double SuccessRate, double P99Latency, double P95Latency, double P90Latency) GetMetrics()
        {
            lock (_lock)
            {
                if (_records.Count == 0)
                    return (100.0, 0, 0, 0);

                var successCount = _records.Count(r => r.Success);
                var successRate = (successCount / (double)_records.Count) * 100;

                var latencies = _records.Select(r => r.Latency).OrderBy(x => x).ToList();
                var p99 = GetPercentile(latencies, 0.99);
                var p95 = GetPercentile(latencies, 0.95);
                var p90 = GetPercentile(latencies, 0.90);

                return (successRate, p99, p95, p90);
            }
        }

        public void RemoveOldEntries(DateTime cutoff)
        {
            lock (_lock)
            {
                _records.RemoveAll(r => r.Time < cutoff);
            }
        }

        private static double GetPercentile(List<double> sorted, double percentile)
        {
            if (sorted.Count == 0) return 0;
            var index = (int)Math.Ceiling(sorted.Count * percentile) - 1;
            return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
        }
    }
}
