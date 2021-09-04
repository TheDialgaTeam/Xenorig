using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;

namespace Xirorig.Utility
{
    internal static class Sha3Utility
    {
        public const int Sha512OutputSize = 512 / 8;

        private static bool _isNativeImplementationAvailable = true;

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
                    if (Sha3Utility_ComputeSha512Hash(inputPtr + inputOffset, inputSize, outputPtr) == 0) throw new CryptographicException();
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
                    if (Sha3Utility_ComputeSha512Hash(inputPtr + inputOffset, inputSize, outputPtr) == 0) throw new CryptographicException();
                }
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                SoftwareComputeSha512Hash(input, inputOffset, inputSize, output);
            }
        }

        [DllImport("xirorig_native")]
        private static extern unsafe int Sha3Utility_ComputeSha512Hash(byte* input, int inputSize, byte* output);

        private static byte[] SoftwareComputeSha512Hash(byte[] input, int inputOffset, int inputSize)
        {
            var result = new byte[Sha512OutputSize];

            var sha3Digest = new Sha3Digest(512);
            sha3Digest.BlockUpdate(input, inputOffset, inputSize);
            sha3Digest.DoFinal(result, 0);

            return result;
        }

        private static void SoftwareComputeSha512Hash(byte[] input, int inputOffset, int inputSize, byte[] output)
        {
            var sha3Digest = new Sha3Digest(512);
            sha3Digest.BlockUpdate(input, inputOffset, inputSize);
            sha3Digest.DoFinal(output, 0);
        }
    }
}