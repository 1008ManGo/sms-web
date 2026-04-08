using Microsoft.Extensions.Logging;

namespace SmppClient.Core;

public class DynamicWindowManager
{
    private readonly int _minWindow;
    private readonly int _maxWindow;
    private readonly TimeSpan _adjustInterval;
    private readonly double _increaseThreshold;
    private readonly double _decreaseThreshold;

    private int _currentWindow;
    private readonly object _lock = new();
    private readonly ILogger<DynamicWindowManager> _logger;
    private readonly System.Timers.Timer _adjustTimer;
    private readonly Queue<double> _recentLatencies = new();

    public DynamicWindowManager(
        int initialWindow,
        int minWindow,
        int maxWindow,
        TimeSpan adjustInterval,
        double increaseThreshold,
        double decreaseThreshold,
        ILogger<DynamicWindowManager> logger)
    {
        _minWindow = minWindow;
        _maxWindow = maxWindow;
        _adjustInterval = adjustInterval;
        _increaseThreshold = increaseThreshold;
        _decreaseThreshold = decreaseThreshold;
        _currentWindow = initialWindow;
        _logger = logger;

        _adjustTimer = new System.Timers.Timer(adjustInterval.TotalMilliseconds);
        _adjustTimer.Elapsed += (_, _) => AdjustWindow();
    }

    public int CurrentWindow
    {
        get { lock (_lock) return _currentWindow; }
    }

    public void Start()
    {
        _adjustTimer.Start();
    }

    public void Stop()
    {
        _adjustTimer.Stop();
    }

    public void RecordLatency(double milliseconds)
    {
        lock (_lock)
        {
            _recentLatencies.Enqueue(milliseconds);
            if (_recentLatencies.Count > 100)
                _recentLatencies.Dequeue();
        }
    }

    private void AdjustWindow()
    {
        lock (_lock)
        {
            if (_recentLatencies.Count == 0)
                return;

            var avgLatency = _recentLatencies.Average();
            var previousWindow = _currentWindow;

            if (avgLatency < _increaseThreshold && _currentWindow < _maxWindow)
            {
                _currentWindow = Math.Min(_maxWindow, _currentWindow + 5);
                _logger.LogInformation(
                    "Window increased: {Prev} -> {Curr} (avg latency: {Latency:F2}ms)",
                    previousWindow, _currentWindow, avgLatency);
            }
            else if (avgLatency > _decreaseThreshold && _currentWindow > _minWindow)
            {
                _currentWindow = Math.Max(_minWindow, _currentWindow - 5);
                _logger.LogInformation(
                    "Window decreased: {Prev} -> {Curr} (avg latency: {Latency:F2}ms)",
                    previousWindow, _currentWindow, avgLatency);
            }
        }
    }

    public (int Min, int Max, int Current) GetWindowInfo()
    {
        lock (_lock)
        {
            return (_minWindow, _maxWindow, _currentWindow);
        }
    }
}
