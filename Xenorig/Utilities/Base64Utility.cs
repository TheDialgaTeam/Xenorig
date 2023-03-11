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
        public static partial int Base64Utility_DecodeLength(in byte input, int inputLength);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int Base64Utility_Encode(in byte input, int inputLength, ref byte output);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int Base64Utility_Decode(in byte input, int inputLength, ref byte output);
    }

    public static int EncodeLength(ReadOnlySpan<byte> value)
    {
        return Native.Base64Utility_EncodeLength(value.Length);
    }

    public static int DecodeLength(ReadOnlySpan<byte> value)
    {
        return Native.Base64Utility_DecodeLength(in MemoryMarshal.GetReference(value), value.Length);
    }

    public static int Encode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Native.Base64Utility_Encode(in MemoryMarshal.GetReference(input), input.Length, ref MemoryMarshal.GetReference(output));
    }

    public static int Decode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Native.Base64Utility_Decode(in MemoryMarshal.GetReference(input), input.Length, ref MemoryMarshal.GetReference(output));
    }
}