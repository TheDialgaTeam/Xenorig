using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

public static class UnsafeUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetRef<T>(this Span<T> span, nint index)
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T GetRef<T>(this ReadOnlySpan<T> span, nint index)
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetRef<T>(this T[] array, nint index)
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(this Span<T> span)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length * Unsafe.SizeOf<T>());
    }
}