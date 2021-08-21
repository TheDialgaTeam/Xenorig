using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using LZ4;

namespace Xirorig.Utility
{
    internal static class Lz4CompressNonceIvUtility
    {
        private static bool? _useSoftwareImplementation;

        public static unsafe bool IsNativeImplementationAvailable
        {
            get
            {
                if (_useSoftwareImplementation != null) return !_useSoftwareImplementation.GetValueOrDefault(false);

                try
                {
                    fixed (byte* testPtr = RandomNumberGenerator.GetBytes(64), outputPtr = new byte[getMaxCompressSize(64) + 8])
                    {
                        doLz4CompressNonceIvMiningInstruction(testPtr, 64, outputPtr);
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

        public static unsafe byte[] DoLz4CompressNonceIvMiningInstruction(byte[] pocShareIv)
        {
            if (!IsNativeImplementationAvailable) return SoftwareDoLz4CompressNonceIvMiningInstruction(pocShareIv);

            try
            {
                var pocShareIvLength = pocShareIv.Length;
                Span<byte> output = stackalloc byte[getMaxCompressSize(pocShareIvLength) + 8];

                fixed (byte* pocShareIvPtr = pocShareIv, outputPtr = output)
                {
                    var size = doLz4CompressNonceIvMiningInstruction(pocShareIvPtr, pocShareIvLength, outputPtr);
                    return output[..size].ToArray();
                }
            }
            catch (Exception)
            {
                return SoftwareDoLz4CompressNonceIvMiningInstruction(pocShareIv);
            }
        }

        [DllImport("xirorig_native")]
        private static extern int getMaxCompressSize(int inputSize);

        [DllImport("xirorig_native")]
        private static extern unsafe int doLz4CompressNonceIvMiningInstruction(byte* input, int inputSize, byte* output);

        private static byte[] SoftwareDoLz4CompressNonceIvMiningInstruction(byte[] pocShareIv)
        {
            return LZ4Codec.Wrap(pocShareIv, 0, pocShareIv.Length);
        }
    }
}