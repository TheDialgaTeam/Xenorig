﻿using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

internal static class SymmetricAlgorithmUtility
{
    private static class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern int SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(in byte key, in byte iv, in byte source, int sourceLength, in byte destination, bool padding);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(in byte key, in byte iv, in byte source, int sourceLength, in byte destination);
    }

    public static int Encrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(MemoryMarshal.GetReference(key), MemoryMarshal.GetReference(iv), MemoryMarshal.GetReference(source), source.Length, MemoryMarshal.GetReference(destination), true);
    }

    public static int Decrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(MemoryMarshal.GetReference(key), MemoryMarshal.GetReference(iv), MemoryMarshal.GetReference(source), source.Length, MemoryMarshal.GetReference(destination));
    }
}