using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Xirorig.Utilities
{
    internal static class Sha2Utility
    {
        private static class Native
        {
            [DllImport(Program.XirorigNativeLibrary)]
            public static extern int Sha2Utility_TryComputeSha256Hash(in byte source, int sourceLength, in byte destination, out int bytesWritten);

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern int Sha2Utility_TryComputeSha512Hash(in byte source, int sourceLength, in byte destination, out int bytesWritten);
        }

        private const int Sha256OutputSize = 256 / 8;
        private const int Sha512OutputSize = 512 / 8;

        private static bool _isNativeImplementationAvailable = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha256Hash(byte[] source)
        {
            return ComputeSha256Hash(source.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha256Hash(byte[] source, int offset, int length)
        {
            return ComputeSha256Hash(source.AsSpan(offset, length));
        }

        public static byte[] ComputeSha256Hash(ReadOnlySpan<byte> source)
        {
            if (!_isNativeImplementationAvailable) return SoftwareComputeSha256Hash(source);

            try
            {
                var result = new byte[Sha256OutputSize];

                if (Native.Sha2Utility_TryComputeSha256Hash(MemoryMarshal.GetReference(source), source.Length, Unsafe.AsRef(result[0]), out var _) == 0) throw new CryptographicException();

                return result;
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                return SoftwareComputeSha256Hash(source);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryComputeSha256Hash(byte[] source, byte[] destination, out int bytesWritten)
        {
            return TryComputeSha256Hash(source.AsSpan(), destination.AsSpan(), out bytesWritten);
        }

        public static bool TryComputeSha256Hash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (!_isNativeImplementationAvailable) return SoftwareTryComputeSha256Hash(source, destination, out bytesWritten);

            try
            {
                return Native.Sha2Utility_TryComputeSha256Hash(MemoryMarshal.GetReference(source), source.Length, MemoryMarshal.GetReference(destination), out bytesWritten) == 1;
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                return SoftwareTryComputeSha256Hash(source, destination, out bytesWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha512Hash(byte[] source)
        {
            return ComputeSha512Hash(source.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha512Hash(byte[] source, int offset, int length)
        {
            return ComputeSha512Hash(source.AsSpan(offset, length));
        }

        public static byte[] ComputeSha512Hash(ReadOnlySpan<byte> source)
        {
            if (!_isNativeImplementationAvailable) return SoftwareComputeSha512Hash(source);

            try
            {
                var result = new byte[Sha512OutputSize];

                if (Native.Sha2Utility_TryComputeSha512Hash(MemoryMarshal.GetReference(source), source.Length, Unsafe.AsRef(result[0]), out var _) == 0) throw new CryptographicException();

                return result;
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                return SoftwareComputeSha512Hash(source);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryComputeSha512Hash(byte[] source, byte[] destination, out int bytesWritten)
        {
            return TryComputeSha512Hash(source.AsSpan(), destination.AsSpan(), out bytesWritten);
        }

        public static bool TryComputeSha512Hash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (!_isNativeImplementationAvailable) return SoftwareTryComputeSha512Hash(source, destination, out bytesWritten);

            try
            {
                return Native.Sha2Utility_TryComputeSha512Hash(MemoryMarshal.GetReference(source), source.Length, MemoryMarshal.GetReference(destination), out bytesWritten) == 1;
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                return SoftwareTryComputeSha512Hash(source, destination, out bytesWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] SoftwareComputeSha256Hash(ReadOnlySpan<byte> source)
        {
            return SHA256.HashData(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SoftwareTryComputeSha256Hash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return SHA256.TryHashData(source, destination, out bytesWritten);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] SoftwareComputeSha512Hash(ReadOnlySpan<byte> source)
        {
            return SHA512.HashData(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SoftwareTryComputeSha512Hash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return SHA512.TryHashData(source, destination, out bytesWritten);
        }
    }
}