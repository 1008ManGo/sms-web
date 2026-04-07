namespace SmppClient.Core;

public class SequenceManager
{
    private uint _currentSequence = 0;
    private readonly object _lock = new();
    private const uint MaxSequence = 0xFFFFFFFF;

    public uint Next()
    {
        lock (_lock)
        {
            _currentSequence++;
            if (_currentSequence >= MaxSequence)
                _currentSequence = 1;
            return _currentSequence;
        }
    }

    public uint Current => _currentSequence;

    public void Reset()
    {
        lock (_lock)
        {
            _currentSequence = 0;
        }
    }
}
