using System;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

public static partial class SymmetricAlgorithmUtility
{
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination, [MarshalAs(UnmanagedType.Bool)] bool padding);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8_Unsafe(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination, [MarshalAs(UnmanagedType.Bool)] bool padding);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8_Unsafe(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination);
    }

    public static int Encrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(key, iv, source, source.Length, destination, true);
    }

    public static int Decrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(key, iv, source, source.Length, destination);
    }
    
    public static int Encrypt_AES_256_CFB_8_Unsafe(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8_Unsafe(key, iv, source, source.Length, destination, true);
    }

    public static int Decrypt_AES_256_CFB_8_Unsafe(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8_Unsafe(key, iv, source, source.Length, destination);
    }
}