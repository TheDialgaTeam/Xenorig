using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

internal static class MessageDigestUtility
{
    private static class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern int MessageDigestUtility_ComputeSha2_256Hash(in byte source, int length, in byte destination);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int MessageDigestUtility_ComputeSha2_512Hash(in byte source, int length, in byte destination);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int MessageDigestUtility_ComputeSha3_512Hash(in byte source, int length, in byte destination);
    }

    public static int ComputeSha2_256Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha2_256Hash(MemoryMarshal.GetReference(source), source.Length, MemoryMarshal.GetReference(destination));
    }

    public static int ComputeSha2_512Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha2_512Hash(MemoryMarshal.GetReference(source), source.Length, MemoryMarshal.GetReference(destination));
    }

    public static int ComputeSha3_512Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha3_512Hash(MemoryMarshal.GetReference(source), source.Length, MemoryMarshal.GetReference(destination));
    }
}