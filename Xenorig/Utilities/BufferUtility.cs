using System;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

public static partial class BufferUtility
{
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial void BufferUtility_MemoryCopy_Byte(ref byte destination, in byte source, int length);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial void BufferUtility_MemoryCopy_Int(ref int destination, in int source, int length);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial void BufferUtility_MemoryCopy_Long(ref long destination, in long source, int length);
    }

    public static void MemoryCopy(ReadOnlySpan<byte> source, Span<byte> destination, int length)
    {
        Native.BufferUtility_MemoryCopy_Byte(ref MemoryMarshal.GetReference(destination), in MemoryMarshal.GetReference(source), length);
    }
    
    public static void MemoryCopy(ReadOnlySpan<int> source, Span<int> destination, int length)
    {
        Native.BufferUtility_MemoryCopy_Int(ref MemoryMarshal.GetReference(destination), in MemoryMarshal.GetReference(source), length);
    }

    public static void MemoryCopy(ReadOnlySpan<long> source, Span<long> destination, int length)
    {
        Native.BufferUtility_MemoryCopy_Long(ref MemoryMarshal.GetReference(destination), in MemoryMarshal.GetReference(source), length);
    }
}