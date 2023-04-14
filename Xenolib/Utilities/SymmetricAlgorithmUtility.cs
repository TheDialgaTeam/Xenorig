using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Xenolib.Utilities;

public static partial class SymmetricAlgorithmUtility
{
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination);
        
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, int sourceLength, Span<byte> destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encrypt_AES_128_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(key, iv, source, source.Length, destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encrypt_AES_192_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(key, iv, source, source.Length, destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encrypt_AES_256_CBC(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(key, iv, source, source.Length, destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(key, iv, source, source.Length, destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Decrypt_AES_256_CFB_8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Native.SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(key, iv, source, source.Length, destination);
    }
}