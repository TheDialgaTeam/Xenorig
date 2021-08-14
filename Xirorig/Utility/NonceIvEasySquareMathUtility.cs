using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xirorig.Network.Api.Models;

namespace Xirorig.Utility
{
    internal static class NonceIvEasySquareMathUtility
    {
        private static bool? _useSoftwareImplementation;

        public static unsafe bool IsNativeImplementationAvailable
        {
            get
            {
                if (_useSoftwareImplementation != null) return !_useSoftwareImplementation.GetValueOrDefault(false);

                try
                {
                    fixed (byte* testPtr = RandomNumberGenerator.GetBytes(64))
                    {
                        doNonceIvEasySquareMathMiningInstruction(1, 1, 0, long.MaxValue, 1, testPtr, 64, testPtr, 64, testPtr, 64);
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

        public static unsafe bool DoNonceIvEasySquareMathMiningInstruction(BlockTemplate blockTemplate, ref byte[] pocShareIv, byte[] previousFinalBlockTransactionHashKey)
        {
            if (!IsNativeImplementationAvailable) return NativeDoNonceIvEasySquareMathMiningInstruction(blockTemplate, ref pocShareIv, previousFinalBlockTransactionHashKey);

            try
            {
                var minerSettings = blockTemplate.MiningSettings;
                var blockDifficultyBytes = blockTemplate.CurrentBlockDifficulty.ToByteArray();

                fixed (byte* pocShareIvPtr = pocShareIv, previousFinalBlockTransactionHashKeyPtr = previousFinalBlockTransactionHashKey, blockDifficultyBytesPtr = blockDifficultyBytes)
                {
                    var result = doNonceIvEasySquareMathMiningInstruction(
                        minerSettings.PocShareNonceMaxSquareRetry,
                        minerSettings.PocShareNonceNoSquareFoundShaRounds,
                        minerSettings.PocShareNonceMin,
                        minerSettings.PocShareNonceMax,
                        blockTemplate.CurrentBlockHeight,
                        pocShareIvPtr,
                        pocShareIv.Length,
                        previousFinalBlockTransactionHashKeyPtr,
                        previousFinalBlockTransactionHashKey.Length,
                        blockDifficultyBytesPtr,
                        blockDifficultyBytes.Length
                    );

                    if (result == 0) return false;

                    Array.Resize(ref pocShareIv, 8);

                    return true;
                }
            }
            catch (Exception)
            {
                return NativeDoNonceIvEasySquareMathMiningInstruction(blockTemplate, ref pocShareIv, previousFinalBlockTransactionHashKey);
            }
        }

        [DllImport("xirorig_native")]
        private static extern unsafe int doNonceIvEasySquareMathMiningInstruction(int pocShareNonceMaxSquareRetry, int pocShareNonceNoSquareFoundShaRounds, long pocShareNonceMin, long pocShareNonceMax, long currentBlockHeight, byte* pocShareIv, int pocShareIvLength, byte* previousFinalBlockTransactionHashKey, int previousFinalBlockTransactionHashKeyLength, byte* blockDifficulty, int blockDifficultyLength);

        private static bool NativeDoNonceIvEasySquareMathMiningInstruction(BlockTemplate blockTemplate, ref byte[] pocShareIv, byte[] previousFinalBlockTransactionHashKey)
        {
            var totalRetry = 0;
            var arrayPool = ArrayPool<byte>.Shared;

            var blockDifficultyBytes = blockTemplate.CurrentBlockDifficulty.ToByteArray();
            var minerSettings = blockTemplate.MiningSettings;

            var newNonceGenerated = false;
            var newNonce = 0L;

            while (totalRetry < minerSettings.PocShareNonceMaxSquareRetry)
            {
                var minimumLength = pocShareIv.Length + previousFinalBlockTransactionHashKey.Length + 8 + blockDifficultyBytes.Length;
                var pocShareWorkToDoBytes = arrayPool.Rent(minimumLength);

                try
                {
                    Buffer.BlockCopy(pocShareIv, 0, pocShareWorkToDoBytes, 0, pocShareIv.Length);
                    Buffer.BlockCopy(blockDifficultyBytes, 0, pocShareWorkToDoBytes, pocShareIv.Length, blockDifficultyBytes.Length);

                    var offset = pocShareIv.Length + blockDifficultyBytes.Length;

                    // Block Height
                    pocShareWorkToDoBytes[offset] = (byte) (blockTemplate.CurrentBlockHeight & 0xFF);
                    pocShareWorkToDoBytes[offset + 1] = (byte) ((blockTemplate.CurrentBlockHeight & 0xFF_00) >> 8);
                    pocShareWorkToDoBytes[offset + 2] = (byte) ((blockTemplate.CurrentBlockHeight & 0xFF_00_00) >> 16);
                    pocShareWorkToDoBytes[offset + 3] = (byte) ((blockTemplate.CurrentBlockHeight & 0xFF_00_00_00) >> 24);
                    pocShareWorkToDoBytes[offset + 4] = (byte) ((blockTemplate.CurrentBlockHeight & 0xFF_00_00_00_00) >> 32);
                    pocShareWorkToDoBytes[offset + 5] = (byte) ((blockTemplate.CurrentBlockHeight & 0xFF_00_00_00_00_00) >> 40);
                    pocShareWorkToDoBytes[offset + 6] = (byte) ((blockTemplate.CurrentBlockHeight & 0xFF_00_00_00_00_00_00) >> 48);
                    pocShareWorkToDoBytes[offset + 7] = (byte) ((blockTemplate.CurrentBlockHeight & 0x7F_00_00_00_00_00_00_00) >> 56);

                    Buffer.BlockCopy(previousFinalBlockTransactionHashKey, 0, pocShareWorkToDoBytes, offset + 8, previousFinalBlockTransactionHashKey.Length);

                    Sha3Utility.ComputeSha3512Hash(pocShareWorkToDoBytes, 0, minimumLength, pocShareWorkToDoBytes);

                    for (var i = 0; i < 64; i += 8)
                    {
                        var x1 = pocShareWorkToDoBytes[i] + (pocShareWorkToDoBytes[i + 1] << 8);
                        var y1 = pocShareWorkToDoBytes[i + 1] + (pocShareWorkToDoBytes[i] << 8);

                        var x2 = pocShareWorkToDoBytes[i + 2] + (pocShareWorkToDoBytes[i + 3] << 8);
                        var y2 = pocShareWorkToDoBytes[i + 3] + (pocShareWorkToDoBytes[i + 2] << 8);

                        var x3 = pocShareWorkToDoBytes[i + 4] + (pocShareWorkToDoBytes[i + 5] << 8);
                        var y3 = pocShareWorkToDoBytes[i + 5] + (pocShareWorkToDoBytes[i + 4] << 8);

                        var x4 = pocShareWorkToDoBytes[i + 6] + (pocShareWorkToDoBytes[i + 7] << 8);
                        var y4 = pocShareWorkToDoBytes[i + 7] + (pocShareWorkToDoBytes[i + 6] << 8);

                        if (Math.Abs(y2 - y1) == Math.Abs(x3 - x1) && Math.Abs(x2 - x1) == Math.Abs(y3 - y1) && Math.Abs(y2 - y4) == Math.Abs(x3 - x4) && Math.Abs(x2 - x4) == Math.Abs(y3 - y4) ||
                            Math.Abs(y2 - y1) == Math.Abs(x4 - x1) && Math.Abs(x2 - x1) == Math.Abs(y4 - y3) && Math.Abs(y2 - y3) == Math.Abs(x4 - x3) && Math.Abs(x2 - x3) == Math.Abs(y4 - y3) ||
                            Math.Abs(y3 - y1) == Math.Abs(x4 - x1) && Math.Abs(x3 - x1) == Math.Abs(y4 - y1) && Math.Abs(y3 - y2) == Math.Abs(x4 - x2) && Math.Abs(x3 - x2) == Math.Abs(y4 - y2))
                        {
                            newNonce = (byte) (pocShareWorkToDoBytes[i] + pocShareWorkToDoBytes[i]) + ((byte) (pocShareWorkToDoBytes[i + 2] + pocShareWorkToDoBytes[i + 2]) << 8) + ((byte) (pocShareWorkToDoBytes[i + 4] + pocShareWorkToDoBytes[i + 4]) << 16) + ((uint) (byte) (pocShareWorkToDoBytes[i + 6] + pocShareWorkToDoBytes[i + 6]) << 24);
                            newNonceGenerated = true;
                            break;
                        }
                    }

                    if (newNonceGenerated) break;

                    pocShareIv = Sha3Utility.ComputeSha3512Hash(pocShareIv);

                    totalRetry++;
                }
                finally
                {
                    arrayPool.Return(pocShareWorkToDoBytes);
                }
            }

            if (!newNonceGenerated)
            {
                for (var i = 0; i < minerSettings.PocShareNonceNoSquareFoundShaRounds; i++)
                {
                    pocShareIv = Sha3Utility.ComputeSha3512Hash(pocShareIv);
                }

                newNonce = pocShareIv[0] + (pocShareIv[1] << 8) + (pocShareIv[2] << 16) + ((uint) pocShareIv[3] << 24);
            }

            if (newNonce < minerSettings.PocShareNonceMin || newNonce > minerSettings.PocShareNonceMax) return false;

            Array.Resize(ref pocShareIv, 8);

            pocShareIv[0] = (byte) (newNonce & 0xFF);
            pocShareIv[1] = (byte) ((newNonce & 0xFF_00) >> 8);
            pocShareIv[2] = (byte) ((newNonce & 0xFF_00_00) >> 16);
            pocShareIv[3] = (byte) ((newNonce & 0xFF_00_00_00) >> 24);
            pocShareIv[4] = (byte) ((newNonce & 0xFF_00_00_00_00) >> 32);
            pocShareIv[5] = (byte) ((newNonce & 0xFF_00_00_00_00_00) >> 40);
            pocShareIv[6] = (byte) ((newNonce & 0xFF_00_00_00_00_00_00) >> 48);
            pocShareIv[7] = (byte) ((newNonce & 0x7F_00_00_00_00_00_00_00) >> 56);

            return true;
        }
    }
}