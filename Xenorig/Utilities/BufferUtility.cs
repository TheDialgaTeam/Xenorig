using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

internal static class BufferUtility
{
    private static class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern void BufferUtility_MemoryCopy_Int(in int destination, in int source, int length);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern void BufferUtility_MemoryCopy_Long(in long destination, in long source, int length);
    }

    public static void MemoryCopy(ReadOnlySpan<int> source, Span<int> destination, int length)
    {
        Native.BufferUtility_MemoryCopy_Long(MemoryMarshal.GetReference(destination), MemoryMarshal.GetReference(source), length);
    }

    public static void MemoryCopy(ReadOnlySpan<long> source, Span<long> destination, int length)
    {
        Native.BufferUtility_MemoryCopy_Long(MemoryMarshal.GetReference(destination), MemoryMarshal.GetReference(source), length);
    }
}