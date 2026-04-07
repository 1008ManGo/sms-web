namespace SmppClient.Core;

public class WindowManager
{
    private readonly int _maxWindow;
    private readonly object _lock = new();
    private int _pendingCount = 0;
    private readonly Queue<TaskCompletionSource<bool>> _waitQueue = new();
    private readonly ILogger<WindowManager> _logger;

    public WindowManager(int maxWindow, ILogger<WindowManager> logger)
    {
        _maxWindow = maxWindow;
        _logger = logger;
    }

    public async Task<bool> AcquireAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_pendingCount < _maxWindow)
            {
                _pendingCount++;
                _logger.LogDebug("Window acquired, pending: {Pending}/{Max}", _pendingCount, _maxWindow);
                return true;
            }

            var tcs = new TaskCompletionSource<bool>();
            _waitQueue.Enqueue(tcs);
            return false;
        }
    }

    public bool TryAcquire()
    {
        lock (_lock)
        {
            if (_pendingCount < _maxWindow)
            {
                _pendingCount++;
                return true;
            }
            return false;
        }
    }

    public void Release()
    {
        lock (_lock)
        {
            if (_pendingCount > 0)
            {
                _pendingCount--;
                _logger.LogDebug("Window released, pending: {Pending}/{Max}", _pendingCount, _maxWindow);

                while (_waitQueue.Count > 0 && _pendingCount < _maxWindow)
                {
                    var tcs = _waitQueue.Dequeue();
                    _pendingCount++;
                    tcs.SetResult(true);
                }
            }
        }
    }

    public int PendingCount
    {
        get { lock (_lock) return _pendingCount; }
    }

    public double UsagePercentage
    {
        get { lock (_lock) return (double)_pendingCount / _maxWindow * 100; }
    }

    public bool IsFull
    {
        get { lock (_lock) return _pendingCount >= _maxWindow; }
    }

    public int MaxWindow => _maxWindow;
}
