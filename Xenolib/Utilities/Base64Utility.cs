using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Xenolib.Utilities;

public static partial class Base64Utility
{
    [UnsupportedOSPlatform("browser")]
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int Base64Utility_EncodeLength(int inputLength);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int Base64Utility_DecodeLength(ReadOnlySpan<byte> input, int inputLength);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int Base64Utility_Encode(ReadOnlySpan<byte> input, int inputLength, Span<byte> output);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int Base64Utility_Decode(ReadOnlySpan<byte> input, int inputLength, Span<byte> output);
    }

    [UnsupportedOSPlatform("browser")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeLength(ReadOnlySpan<byte> value)
    {
        return Native.Base64Utility_EncodeLength(value.Length);
    }

    [UnsupportedOSPlatform("browser")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeLength(ReadOnlySpan<byte> value)
    {
        return Native.Base64Utility_DecodeLength(value, value.Length);
    }

    [UnsupportedOSPlatform("browser")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Native.Base64Utility_Encode(input, input.Length, output);
    }

    [UnsupportedOSPlatform("browser")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Decode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Native.Base64Utility_Decode(input, input.Length, output);
    }
}