using System;
using System.Buffers;

namespace Xenorig.Utilities.Buffer;

public sealed class ArrayPoolOwner<T> : IMemoryOwner<T>
{
    public Span<T> Span => _array.AsSpan(0, _size);

    public Memory<T> Memory => _array.AsMemory(0, _size);

    private T[]? _array;
    private readonly int _size;

    private ArrayPoolOwner(int size)
    {
        _array = ArrayPool<T>.Shared.Rent(size);
        _size = size;
    }

    public static ArrayPoolOwner<T> Rent(int size)
    {
        return new ArrayPoolOwner<T>(size);
    }

    public void Dispose()
    {
        if (_array == null) return;
        ArrayPool<T>.Shared.Return(_array);
        _array = null;
    }
}