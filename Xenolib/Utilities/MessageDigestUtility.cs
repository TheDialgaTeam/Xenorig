using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Xenolib.Utilities;

public static partial class MessageDigestUtility
{
    [UnsupportedOSPlatform("browser")]
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha2_256Hash(ReadOnlySpan<byte> source, int length, Span<byte> destination);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha2_512Hash(ReadOnlySpan<byte> source, int length, Span<byte> destination);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha3_512Hash(ReadOnlySpan<byte> source, int length, Span<byte> destination);
    }

    [UnsupportedOSPlatform("browser")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeSha2_256Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha2_256Hash(source, source.Length, destination);
    }

    [UnsupportedOSPlatform("browser")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeSha2_512Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha2_512Hash(source, source.Length, destination);
    }

    [UnsupportedOSPlatform("browser")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeSha3_512Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha3_512Hash(source, source.Length, destination);
    }
}