using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;

namespace Xirorig.Utility
{
    internal static class Sha2Utility
    {
        public const int Sha256OutputSize = 256 / 8;
        public const int Sha512OutputSize = 512 / 8;

        private static bool _isNativeImplementationAvailable = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha256Hash(byte[] input)
        {
            return ComputeSha256Hash(input, 0, input.Length);
        }

        public static unsafe byte[] ComputeSha256Hash(byte[] input, int inputOffset, int inputSize)
        {
            if (!_isNativeImplementationAvailable) SoftwareComputeSha256Hash(input, inputOffset, inputSize);

            try
            {
                var result = new byte[Sha256OutputSize];

                fixed (byte* inputPtr = input, outputPtr = result)
                {
                    if (Sha2Utility_ComputeSha256Hash(inputPtr + inputOffset, inputSize, outputPtr) == 0) throw new CryptographicException();
                }

                return result;
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                return SoftwareComputeSha256Hash(input, inputOffset, inputSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeSha256Hash(byte[] input, byte[] output)
        {
            ComputeSha256Hash(input, 0, input.Length, output);
        }

        public static unsafe void ComputeSha256Hash(byte[] input, int inputOffset, int inputSize, byte[] output)
        {
            if (output.Length < Sha256OutputSize) throw new ArgumentException($"{nameof(output)} length is too small.");

            if (!_isNativeImplementationAvailable)
            {
                SoftwareComputeSha256Hash(input, inputOffset, inputSize, output);
                return;
            }

            try
            {
                fixed (byte* inputPtr = input, outputPtr = output)
                {
                    if (Sha2Utility_ComputeSha256Hash(inputPtr + inputOffset, inputSize, outputPtr) == 0) throw new CryptographicException();
                }
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                SoftwareComputeSha256Hash(input, inputOffset, inputSize, output);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha512Hash(byte[] input)
        {
            return ComputeSha512Hash(input, 0, input.Length);
        }

        public static unsafe byte[] ComputeSha512Hash(byte[] input, int inputOffset, int inputSize)
        {
            if (!_isNativeImplementationAvailable) SoftwareComputeSha512Hash(input, inputOffset, inputSize);

            try
            {
                var result = new byte[Sha512OutputSize];

                fixed (byte* inputPtr = input, outputPtr = result)
                {
                    if (Sha2Utility_ComputeSha512Hash(inputPtr + inputOffset, inputSize, outputPtr) == 0) throw new CryptographicException();
                }

                return result;
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                return SoftwareComputeSha512Hash(input, inputOffset, inputSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeSha512Hash(byte[] input, byte[] output)
        {
            ComputeSha512Hash(input, 0, input.Length, output);
        }

        public static unsafe void ComputeSha512Hash(byte[] input, int inputOffset, int inputSize, byte[] output)
        {
            if (output.Length < Sha512OutputSize) throw new ArgumentException($"{nameof(output)} length is too small.");

            if (!_isNativeImplementationAvailable)
            {
                SoftwareComputeSha512Hash(input, inputOffset, inputSize, output);
                return;
            }

            try
            {
                fixed (byte* inputPtr = input, outputPtr = output)
                {
                    if (Sha2Utility_ComputeSha512Hash(inputPtr + inputOffset, inputSize, outputPtr) == 0) throw new CryptographicException();
                }
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                SoftwareComputeSha512Hash(input, inputOffset, inputSize, output);
            }
        }

        [DllImport("xirorig_native")]
        private static extern unsafe int Sha2Utility_ComputeSha256Hash(byte* input, int inputSize, byte* output);

        [DllImport("xirorig_native")]
        private static extern unsafe int Sha2Utility_ComputeSha512Hash(byte* input, int inputSize, byte* output);

        private static byte[] SoftwareComputeSha256Hash(byte[] input, int inputOffset, int inputSize)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(input, inputOffset, inputSize);
        }

        private static void SoftwareComputeSha256Hash(byte[] input, int inputOffset, int inputSize, byte[] output)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(input, inputOffset, inputSize);
            Buffer.BlockCopy(hash, 0, output, 0, hash.Length);
        }

        private static byte[] SoftwareComputeSha512Hash(byte[] input, int inputOffset, int inputSize)
        {
            using var sha512 = SHA512.Create();
            return sha512.ComputeHash(input, inputOffset, inputSize);
        }

        private static void SoftwareComputeSha512Hash(byte[] input, int inputOffset, int inputSize, byte[] output)
        {
            using var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(input, inputOffset, inputSize);
            Buffer.BlockCopy(hash, 0, output, 0, hash.Length);
        }
    }
}