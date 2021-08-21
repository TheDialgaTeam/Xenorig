using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;

namespace Xirorig.Utility
{
    internal static class Sha3Utility
    {
        public const int Sha3512OutputSize = 512 / 8;

        private static bool? _useSoftwareImplementation;

        public static unsafe bool IsNativeImplementationAvailable
        {
            get
            {
                if (_useSoftwareImplementation != null) return !_useSoftwareImplementation.GetValueOrDefault(false);

                try
                {
                    fixed (byte* testPtr = RandomNumberGenerator.GetBytes(Sha3512OutputSize))
                    {
                        computeSha3512Hash(testPtr, Sha3512OutputSize, testPtr);
                        _useSoftwareImplementation = false;
                    }
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
                {
                    _useSoftwareImplementation = true;
                }

                return !_useSoftwareImplementation.GetValueOrDefault(false);
            }
        }

        public static unsafe byte[] ComputeSha3512Hash(byte[] input)
        {
            if (!IsNativeImplementationAvailable) return SoftwareComputeSha3512Hash(input);

            try
            {
                var result = new byte[Sha3512OutputSize];

                fixed (byte* inputPtr = input, outputPtr = result)
                {
                    if (computeSha3512Hash(inputPtr, input.Length, outputPtr) == 0) throw new Exception();
                }

                return result;
            }
            catch (Exception)
            {
                return SoftwareComputeSha3512Hash(input);
            }
        }

        public static unsafe byte[] ComputeSha3512Hash(byte[] input, int inputOffset, int inputSize)
        {
            if (!IsNativeImplementationAvailable) SoftwareComputeSha3512Hash(input, inputOffset, inputSize);

            try
            {
                var result = new byte[Sha3512OutputSize];

                fixed (byte* inputPtr = input, outputPtr = result)
                {
                    if (computeSha3512Hash(inputPtr + inputOffset, inputSize, outputPtr) == 0) throw new Exception();
                }

                return result;
            }
            catch (Exception)
            {
                return SoftwareComputeSha3512Hash(input, inputOffset, inputSize);
            }
        }

        public static unsafe void ComputeSha3512Hash(byte[] input, byte[] output)
        {
            if (output.Length < Sha3512OutputSize) throw new ArgumentException($"{nameof(output)} length is too small.");

            if (!IsNativeImplementationAvailable)
            {
                SoftwareComputeSha3512Hash(input, output);
                return;
            }

            try
            {
                fixed (byte* inputPtr = input, outputPtr = output)
                {
                    if (computeSha3512Hash(inputPtr, input.Length, outputPtr) == 0) throw new Exception();
                }
            }
            catch (Exception)
            {
                SoftwareComputeSha3512Hash(input, output);
            }
        }

        public static unsafe void ComputeSha3512Hash(byte[] input, int inputOffset, int inputSize, byte[] output)
        {
            if (output.Length < Sha3512OutputSize) throw new ArgumentException($"{nameof(output)} length is too small.");

            if (!IsNativeImplementationAvailable)
            {
                SoftwareComputeSha3512Hash(input, inputOffset, inputSize, output);
                return;
            }

            try
            {
                fixed (byte* inputPtr = input, outputPtr = output)
                {
                    if (computeSha3512Hash(inputPtr + inputOffset, inputSize, outputPtr) == 0) throw new Exception();
                }
            }
            catch (Exception)
            {
                SoftwareComputeSha3512Hash(input, inputOffset, inputSize, output);
            }
        }

        [DllImport("xirorig_native")]
        private static extern unsafe int computeSha3512Hash(byte* input, int inputSize, byte* output);

        private static byte[] SoftwareComputeSha3512Hash(byte[] input)
        {
            var result = new byte[Sha3512OutputSize];

            var sha3Digest = new Sha3Digest(512);
            sha3Digest.BlockUpdate(input, 0, input.Length);
            sha3Digest.DoFinal(result, 0);

            return result;
        }

        private static byte[] SoftwareComputeSha3512Hash(byte[] input, int inputOffset, int inputSize)
        {
            var result = new byte[Sha3512OutputSize];

            var sha3Digest = new Sha3Digest(512);
            sha3Digest.BlockUpdate(input, inputOffset, inputSize);
            sha3Digest.DoFinal(result, 0);

            return result;
        }

        private static void SoftwareComputeSha3512Hash(byte[] input, byte[] output)
        {
            var sha3Digest = new Sha3Digest(512);
            sha3Digest.BlockUpdate(input, 0, input.Length);
            sha3Digest.DoFinal(output, 0);
        }

        private static void SoftwareComputeSha3512Hash(byte[] input, int inputOffset, int inputSize, byte[] output)
        {
            var sha3Digest = new Sha3Digest(512);
            sha3Digest.BlockUpdate(input, inputOffset, inputSize);
            sha3Digest.DoFinal(output, 0);
        }
    }
}