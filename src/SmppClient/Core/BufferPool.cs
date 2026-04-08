using System.Buffers;

namespace SmppClient.Core;

public static class PduBufferPool
{
    private const int DefaultBufferSize = 65536;
    private const int HeaderBufferSize = 16;
    private const int MaxBodySize = 65520;

    private static readonly ArrayPool<byte> SharedPool = ArrayPool<byte>.Shared;

    public static byte[] RentHeader()
    {
        return SharedPool.Rent(HeaderBufferSize);
    }

    public static byte[] RentBody(int minSize = MaxBodySize)
    {
        return SharedPool.Rent(Math.Max(minSize, MaxBodySize));
    }

    public static byte[] RentPdu(int commandLength)
    {
        return SharedPool.Rent((int)commandLength);
    }

    public static void Return(byte[] buffer)
    {
        if (buffer != null && buffer.Length > 0)
        {
            Array.Clear(buffer, 0, buffer.Length);
            SharedPool.Return(buffer);
        }
    }

    public static void ReturnIfNotNull(byte[]? buffer)
    {
        if (buffer != null)
        {
            Return(buffer);
        }
    }
}

public class RecyclableMemoryStreamManager
{
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;
    private const int BlockSize = 4096;

    public static byte[] GetBlock()
    {
        return Pool.Rent(BlockSize);
    }

    public static void ReturnBlock(byte[] block)
    {
        if (block != null)
        {
            Array.Clear(block, 0, block.Length);
            Pool.Return(block);
        }
    }

    public static byte[] GetBuffer(int minimumLength)
    {
        return Pool.Rent(minimumLength);
    }

    public static void ReturnBuffer(byte[] buffer)
    {
        if (buffer != null)
        {
            Array.Clear(buffer, 0, buffer.Length);
            Pool.Return(buffer);
        }
    }
}

public class PooledBuffer : IDisposable
{
    private byte[]? _buffer;
    private bool _disposed;

    public byte[] Buffer => _buffer ?? throw new ObjectDisposedException(nameof(PooledBuffer));
    public int Length => _buffer?.Length ?? 0;

    public PooledBuffer(int size)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(size);
    }

    public Span<byte> AsSpan() => _buffer.AsSpan(0, _buffer?.Length ?? 0);
    public Memory<byte> AsMemory() => _buffer.AsMemory(0, _buffer?.Length ?? 0);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_buffer != null)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null;
        }
    }
}
