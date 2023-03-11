using System;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

public static partial class MessageDigestUtility
{
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha2_256Hash(in byte source, int length, in byte destination);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha2_512Hash(in byte source, int length, in byte destination);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha3_512Hash(in byte source, int length, in byte destination);
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