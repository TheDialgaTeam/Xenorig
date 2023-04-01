using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Xenorig.Utilities;

public static partial class SymmetricAlgorithmUtility
{
    private static partial class Native
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination, [MarshalAs(UnmanagedType.Bool)] bool padding);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination, [MarshalAs(UnmanagedType.Bool)] bool padding);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination, [MarshalAs(UnmanagedType.Bool)] bool padding);

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination, [MarshalAs(UnmanagedType.Bool)] bool padding);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination, [MarshalAs(UnmanagedType.Bool)] bool padding);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encrypt_AES_128_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(key, iv, source, source.Length, destination, true);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encrypt_AES_192_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(key, iv, source, source.Length, destination, true);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encrypt_AES_256_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(key, iv, source, source.Length, destination, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(key, iv, source, source.Length, destination, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Decrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(key, iv, source, source.Length, destination, true);
    }
}