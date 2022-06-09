using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

internal static class BufferUtility
{
    private static class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern void BufferUtility_MemoryCopy_Long(in long destination, in long source, long length);
    }

    public static void MemoryCopy(long[] source, long sourceOffset, long[] destination, long destinationOffset, long length)
    {
        Native.BufferUtility_MemoryCopy_Long(Unsafe.AsRef(destination[destinationOffset]), Unsafe.AsRef(source[sourceOffset]), length);
    }
}