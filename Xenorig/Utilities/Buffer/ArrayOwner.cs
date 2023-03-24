using System;
using System.Buffers;

namespace Xenorig.Utilities.Buffer;

public sealed class ArrayOwner<T> : IDisposable
{
    public Span<T> Span
    {
        get
        {
            ObjectDisposedException.ThrowIf(_array == null, _array);
            return _array.AsSpan(0, _size);
        }
    }

    private T[]? _array;
    private readonly int _size;

    private ArrayOwner(int size)
    {
        _array = ArrayPool<T>.Shared.Rent(size);
        _size = size;
    }

    public static ArrayOwner<T> Rent(int size)
    {
        return new ArrayOwner<T>(size);
    }

    public void Dispose()
    {
        if (_array == null) return;
        ArrayPool<T>.Shared.Return(_array);
        _array = null;
    }
}