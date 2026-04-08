using System.Threading.Channels;

namespace SmppClient.Core;

public class WindowManager
{
    private readonly int _maxWindow;
    private readonly Channel<bool> _slots;
    private int _currentCount;
    private readonly object _countLock = new();
    private readonly ILogger<WindowManager> _logger;

    public WindowManager(int maxWindow, ILogger<WindowManager> logger)
    {
        _maxWindow = maxWindow;
        _logger = logger;

        var options = new BoundedChannelOptions(maxWindow)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };
        _slots = Channel.CreateBounded<bool>(options);

        for (int i = 0; i < maxWindow; i++)
        {
            _slots.Writer.TryWrite(true);
        }
    }

    public async Task<bool> AcquireAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _slots.Reader.ReadAsync(cancellationToken))
            {
                Interlocked.Increment(ref _currentCount);
                _logger.LogDebug("Window acquired, pending: {Pending}/{Max}", _currentCount, _maxWindow);
                return true;
            }
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public bool TryAcquire()
    {
        if (_slots.Reader.TryRead(out _))
        {
            Interlocked.Increment(ref _currentCount);
            _logger.LogDebug("Window acquired (Try), pending: {Pending}/{Max}", _currentCount, _maxWindow);
            return true;
        }
        return false;
    }

    public void Release()
    {
        if (_currentCount > 0)
        {
            Interlocked.Decrement(ref _currentCount);
            _logger.LogDebug("Window released, pending: {Pending}/{Max}", _currentCount, _maxWindow);
        }
        _slots.Writer.TryWrite(true);
    }

    public int PendingCount => _currentCount;

    public double UsagePercentage => (double)_currentCount / _maxWindow * 100;

    public bool IsFull => _currentCount >= _maxWindow;

    public int MaxWindow => _maxWindow;
}

public class LockFreeWindowManager
{
    private readonly int _maxWindow;
    private int _pendingCount;
    private readonly Channel<CompletionSignal> _waitQueue;
    private readonly ILogger<LockFreeWindowManager> _logger;

    private struct CompletionSignal
    {
        public TaskCompletionSource<bool> Task;
    }

    public LockFreeWindowManager(int maxWindow, ILogger<LockFreeWindowManager> logger)
    {
        _maxWindow = maxWindow;
        _logger = logger;
        _pendingCount = 0;

        _waitQueue = Channel.CreateBounded<CompletionSignal>(new BoundedChannelOptions(_maxWindow * 2)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool TryAcquire()
    {
        var current = Interlocked.Increment(ref _pendingCount);
        if (current <= _maxWindow)
        {
            _logger.LogDebug("LockFreeWindow acquired, pending: {Pending}/{Max}", current, _maxWindow);
            return true;
        }

        Interlocked.Decrement(ref _pendingCount);
        return false;
    }

    public void Release()
    {
        var current = Interlocked.Decrement(ref _pendingCount);
        _logger.LogDebug("LockFreeWindow released, pending: {Pending}/{Max}", current, _maxWindow);

        if (_waitQueue.Reader.TryRead(out var signal))
        {
            signal.Task.TrySetResult(true);
        }
    }

    public async Task<bool> AcquireAsync(CancellationToken cancellationToken = default)
    {
        var current = Interlocked.Increment(ref _pendingCount);
        if (current <= _maxWindow)
        {
            _logger.LogDebug("LockFreeWindow acquired (async), pending: {Pending}/{Max}", current, _maxWindow);
            return true;
        }

        Interlocked.Decrement(ref _pendingCount);

        var tcs = new TaskCompletionSource<bool>();
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

        var signal = new CompletionSignal { Task = tcs };
        await _waitQueue.Writer.WriteAsync(signal, cancellationToken);

        try
        {
            Interlocked.Increment(ref _pendingCount);
            return await tcs.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (_waitQueue.Reader.TryRead(out var s))
            {
            }
            return false;
        }
    }

    public int PendingCount => _pendingCount;
    public double UsagePercentage => (double)_pendingCount / _maxWindow * 100;
    public bool IsFull => _pendingCount >= _maxWindow;
    public int MaxWindow => _maxWindow;
}
