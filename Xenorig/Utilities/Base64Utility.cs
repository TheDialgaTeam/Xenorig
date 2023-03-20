using System;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

public static partial class Base64Utility
{
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

    public static int EncodeLength(ReadOnlySpan<byte> value)
    {
        return Native.Base64Utility_EncodeLength(value.Length);
    }

    public static int DecodeLength(ReadOnlySpan<byte> value)
    {
        return Native.Base64Utility_DecodeLength(value, value.Length);
    }

    public static int Encode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Native.Base64Utility_Encode(input, input.Length, output);
    }

    public static int Decode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Native.Base64Utility_Decode(input, input.Length, output);
    }
}