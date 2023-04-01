using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Miner;

public static partial class CpuMinerUtility
{
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(long minValue, long maxValue, Span<long> output);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(long minValue, long maxvalue, Span<long> output, Span<long> output2);

        [LibraryImport(Program.XenoNativeLibrary)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool XenophyteCentralizedAlgorithm_MakeEncryptedShare(ReadOnlySpan<byte> input, int inputLength, Span<byte> encryptedShare, Span<byte> hashEncryptedShare, ReadOnlySpan<byte> xorKey, int xorKeyLength, int aesKeySize, ReadOnlySpan<byte> aesKey, ReadOnlySpan<byte> aesIv, int aesRound);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GenerateEasyBlockNumbers(long minValue, long maxValue, Span<long> output)
    {
        return Native.XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(minValue, maxValue, output);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GenerateNonEasyBlockNumbers(long minValue, long maxValue, Span<long> output, Span<long> output2)
    {
        return Native.XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(minValue, maxValue, output, output2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MakeEncryptedShare(ReadOnlySpan<byte> input, Span<byte> encryptedShare, Span<byte> hashEncryptedShare, ReadOnlySpan<byte> xorKey, ReadOnlySpan<byte> aesKey, ReadOnlySpan<byte> aesIv, int aesRound)
    {
        return Native.XenophyteCentralizedAlgorithm_MakeEncryptedShare(input, input.Length, encryptedShare, hashEncryptedShare, xorKey, xorKey.Length, aesKey.Length * 8, aesKey, aesIv, aesRound);
    }
}