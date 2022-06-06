using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

internal static class Base64Utility
{
    private static class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern int Base64Utility_EncodeLength(int inputLength);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int Base64Utility_DecodeLength(in byte input, int inputLength);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int Base64Utility_Encode(in byte input, int inputLength, in byte output);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int Base64Utility_Decode(in byte input, int inputLength, in byte output);
    }

    public static int EncodeLength(ReadOnlySpan<byte> value)
    {
        return Native.Base64Utility_EncodeLength(value.Length);
    }

    public static int DecodeLength(ReadOnlySpan<byte> value)
    {
        return Native.Base64Utility_DecodeLength(MemoryMarshal.GetReference(value), value.Length);
    }

    public static int Encode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Native.Base64Utility_Encode(MemoryMarshal.GetReference(input), input.Length, MemoryMarshal.GetReference(output));
    }

    public static int Decode(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return Native.Base64Utility_Decode(MemoryMarshal.GetReference(input), input.Length, MemoryMarshal.GetReference(output));
    }
}