using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

internal static class BufferUtility
{
    private static class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern void BufferUtility_MemoryCopy_Int(ref int destination, in int source, int length);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern void BufferUtility_MemoryCopy_Long(ref long destination, in long source, int length);
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