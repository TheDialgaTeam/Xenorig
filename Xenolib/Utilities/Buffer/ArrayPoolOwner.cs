using System.Buffers;

namespace Xenolib.Utilities.Buffer;

public sealed class ArrayPoolOwner<T> : IMemoryOwner<T>
{
    public Span<T> Span => _array.AsSpan(0, _size);

    public Memory<T> Memory => _array.AsMemory(0, _size);

    private T[]? _array;
    private readonly int _size;
    private readonly bool _clearArray;

    private ArrayPoolOwner(int size, bool clearArray)
    {
        _array = ArrayPool<T>.Shared.Rent(size);
        _size = size;
        _clearArray = clearArray;
    }

    public static ArrayPoolOwner<T> Rent(int size, bool clearArray = false)
    {
        return new ArrayPoolOwner<T>(size, clearArray);
    }

    public void Dispose()
    {
        if (_array == null) return;
        ArrayPool<T>.Shared.Return(_array, _clearArray);
        _array = null;
    }
}