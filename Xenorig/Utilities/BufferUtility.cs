using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

public static partial class BufferUtility
{
    private static partial class Native
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial void BufferUtility_MemoryCopy_Byte(Span<byte> destination, ReadOnlySpan<byte> source, int length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial void BufferUtility_MemoryCopy_Int(Span<int> destination, ReadOnlySpan<int> source, int length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial void BufferUtility_MemoryCopy_Long(Span<long> destination, ReadOnlySpan<long> source, int length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemoryCopy(ReadOnlySpan<byte> source, Span<byte> destination, int length)
    {
        Native.BufferUtility_MemoryCopy_Byte(destination, source, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemoryCopy(ReadOnlySpan<int> source, Span<int> destination, int length)
    {
        Native.BufferUtility_MemoryCopy_Int(destination, source, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemoryCopy(ReadOnlySpan<long> source, Span<long> destination, int length)
    {
        Native.BufferUtility_MemoryCopy_Long(destination, source, length);
    }
}