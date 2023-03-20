using System;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

public static partial class MessageDigestUtility
{
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha2_256Hash(ReadOnlySpan<byte> source, int length, Span<byte> destination);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha2_512Hash(ReadOnlySpan<byte> source, int length, Span<byte> destination);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha3_512Hash(ReadOnlySpan<byte> source, int length, Span<byte> destination);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha2_256Hash_Unsafe(ReadOnlySpan<byte> source, int length, Span<byte> destination);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha2_512Hash_Unsafe(ReadOnlySpan<byte> source, int length, Span<byte> destination);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int MessageDigestUtility_ComputeSha3_512Hash_Unsafe(ReadOnlySpan<byte> source, int length, Span<byte> destination);
    }

    public static int ComputeSha2_256Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha2_256Hash(source, source.Length, destination);
    }

    public static int ComputeSha2_512Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha2_512Hash(source, source.Length, destination);
    }

    public static int ComputeSha3_512Hash(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha3_512Hash(source, source.Length, destination);
    }
    
    public static int ComputeSha2_256Hash_Unsafe(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha2_256Hash_Unsafe(source, source.Length, destination);
    }

    public static int ComputeSha2_512Hash_Unsafe(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha2_512Hash_Unsafe(source, source.Length, destination);
    }

    public static int ComputeSha3_512Hash_Unsafe(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.MessageDigestUtility_ComputeSha3_512Hash_Unsafe(source, source.Length, destination);
    }
}