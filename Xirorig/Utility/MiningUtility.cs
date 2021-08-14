using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using LZ4;
using Xirorig.Algorithm;
using Xirorig.Network.Api.Models;

namespace Xirorig.Utility
{
    internal static class MiningUtility
    {
        private const int MaxFloatPrecision = 16777216;
        private const long MaxDoublePrecision = 9007199254740992;

        private const string MathOperatorPlus = "+";
        private const string MathOperatorMinus = "-";
        private const string MathOperatorMultiplication = "*";
        private const string MathOperatorModulo = "%";

        private static readonly BigInteger ShaPowCalculation = BigInteger.Pow(2, 512);

        public static void GeneratePocRandomData(byte[] pocRandomData, byte[] randomNumberBytes, RandomNumberGenerator randomNumberGenerator, BlockTemplate blockTemplate, byte[] walletAddress, long nonce, long timestamp)
        {
            var minerSettings = blockTemplate.MiningSettings;

            // Random numbers must be between 0 to previousBlockTransactionCount.
            int randomNumber, randomNumber2;
            var solutionFound = false;

            do
            {
                var previousBlockTransactionCount = blockTemplate.PreviousBlockTransactionCount;

                randomNumber = GetRandomBetween(randomNumberGenerator, randomNumberBytes, 0, previousBlockTransactionCount);
                randomNumber2 = 0;

                foreach (var supportedMathOperator in minerSettings.MathOperatorList)
                {
                    if (supportedMathOperator == MathOperatorPlus)
                    {
                        // Number will always be within the domain, so we can always break it here.
                        randomNumber2 = previousBlockTransactionCount - randomNumber;
                        solutionFound = true;
                        break;
                    }

                    if (supportedMathOperator == MathOperatorMinus)
                    {
                        if (randomNumber == previousBlockTransactionCount)
                        {
                            randomNumber2 = randomNumber - previousBlockTransactionCount;
                            solutionFound = true;
                            break;
                        }

                        // No solution when randomNumber is smaller than previousBlockTransactionCount.
                    }

                    if (supportedMathOperator == MathOperatorMultiplication)
                    {
                        if (randomNumber > 0)
                        {
                            randomNumber2 = Math.DivRem(previousBlockTransactionCount, randomNumber, out var remainder);

                            if (remainder == 0)
                            {
                                solutionFound = true;
                                break;
                            }
                        }

                        // Division by zero is a taboo, there are no solution if randomNumber = 0.
                    }

                    if (supportedMathOperator == MathOperatorModulo)
                    {
                        if (randomNumber == previousBlockTransactionCount)
                        {
                            randomNumber2 = 0;
                            solutionFound = true;
                            break;
                        }

                        // No solution when randomNumber is not equal to previousBlockTransactionCount.
                    }
                }
            } while (!solutionFound);

            // randomNumber
            pocRandomData[0] = (byte) (randomNumber & 0xFF);
            pocRandomData[1] = (byte) ((randomNumber & 0xFF_00) >> 8);
            pocRandomData[2] = (byte) ((randomNumber & 0xFF_00_00) >> 16);
            pocRandomData[3] = (byte) ((randomNumber & 0x7F_00_00_00) >> 24);

            // randomNumber2
            pocRandomData[4] = (byte) (randomNumber2 & 0xFF);
            pocRandomData[5] = (byte) ((randomNumber2 & 0xFF_00) >> 8);
            pocRandomData[6] = (byte) ((randomNumber2 & 0xFF_00_00) >> 16);
            pocRandomData[7] = (byte) ((randomNumber2 & 0x7F_00_00_00) >> 24);

            // timestamp
            pocRandomData[8] = (byte) (timestamp & 0xFF);
            pocRandomData[9] = (byte) ((timestamp & 0xFF_00) >> 8);
            pocRandomData[10] = (byte) ((timestamp & 0xFF_00_00) >> 16);
            pocRandomData[11] = (byte) ((timestamp & 0xFF_00_00_00) >> 24);
            pocRandomData[12] = (byte) ((timestamp & 0xFF_00_00_00_00) >> 32);
            pocRandomData[13] = (byte) ((timestamp & 0xFF_00_00_00_00_00) >> 40);
            pocRandomData[14] = (byte) ((timestamp & 0xFF_00_00_00_00_00_00) >> 48);
            pocRandomData[15] = (byte) ((timestamp & 0x7F_00_00_00_00_00_00_00) >> 56);

            randomNumberGenerator.GetBytes(pocRandomData, 16, minerSettings.RandomDataShareChecksum);

            Buffer.BlockCopy(walletAddress, 0, pocRandomData, 16 + minerSettings.RandomDataShareChecksum, minerSettings.WalletAddressDataSize);

            var offset = 16 + minerSettings.RandomDataShareChecksum + minerSettings.WalletAddressDataSize;
            var blockHeight = blockTemplate.CurrentBlockHeight;

            // block height
            pocRandomData[offset++] = (byte) (blockHeight & 0xFF);
            pocRandomData[offset++] = (byte) ((blockHeight & 0xFF_00) >> 8);
            pocRandomData[offset++] = (byte) ((blockHeight & 0xFF_00_00) >> 16);
            pocRandomData[offset++] = (byte) ((blockHeight & 0xFF_00_00_00) >> 24);
            pocRandomData[offset++] = (byte) ((blockHeight & 0xFF_00_00_00_00) >> 32);
            pocRandomData[offset++] = (byte) ((blockHeight & 0xFF_00_00_00_00_00) >> 40);
            pocRandomData[offset++] = (byte) ((blockHeight & 0xFF_00_00_00_00_00_00) >> 48);
            pocRandomData[offset++] = (byte) ((blockHeight & 0x7F_00_00_00_00_00_00_00) >> 56);

            // nonce
            pocRandomData[offset] = (byte) (nonce & 0xFF);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00) >> 8);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00) >> 16);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00_00) >> 24);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00_00_00) >> 32);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00_00_00_00) >> 40);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00_00_00_00_00) >> 48);
            pocRandomData[offset] = (byte) ((nonce & 0x7F_00_00_00_00_00_00_00) >> 56);
        }

        public static void UpdatePocRandomData(byte[] pocRandomData, BlockTemplate blockTemplate, long nonce, long timestamp)
        {
            var minerSettings = blockTemplate.MiningSettings;

            // timestamp
            pocRandomData[8] = (byte) (timestamp & 0xFF);
            pocRandomData[9] = (byte) ((timestamp & 0xFF_00) >> 8);
            pocRandomData[10] = (byte) ((timestamp & 0xFF_00_00) >> 16);
            pocRandomData[11] = (byte) ((timestamp & 0xFF_00_00_00) >> 24);
            pocRandomData[12] = (byte) ((timestamp & 0xFF_00_00_00_00) >> 32);
            pocRandomData[13] = (byte) ((timestamp & 0xFF_00_00_00_00_00) >> 40);
            pocRandomData[14] = (byte) ((timestamp & 0xFF_00_00_00_00_00_00) >> 48);
            pocRandomData[15] = (byte) ((timestamp & 0x7F_00_00_00_00_00_00_00) >> 56);

            var offset = 16 + minerSettings.RandomDataShareChecksum + minerSettings.WalletAddressDataSize + 8;

            // nonce
            pocRandomData[offset++] = (byte) (nonce & 0xFF);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00) >> 8);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00) >> 16);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00_00) >> 24);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00_00_00) >> 32);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00_00_00_00) >> 40);
            pocRandomData[offset++] = (byte) ((nonce & 0xFF_00_00_00_00_00_00) >> 48);
            pocRandomData[offset] = (byte) ((nonce & 0x7F_00_00_00_00_00_00_00) >> 56);
        }

        public static bool DoPowShare(MiningPowShare miningPowShare, BlockTemplate blockTemplate, Rijndael rijndael, IAlgorithm algorithm, string walletAddress, long nonce, long timestamp, byte[] pocRandomData, byte[] previousFinalBlockTransactionHashKey)
        {
            var pocShareIv = BitConverter.GetBytes(nonce);

            var minerSettings = blockTemplate.MiningSettings;
            var miningInstructions = minerSettings.MiningInstructions;

            foreach (var miningInstruction in miningInstructions)
            {
                switch (miningInstruction)
                {
                    case MiningInstruction.DoNonceIv:
                    {
                        DoNonceIvMiningInstruction(ref pocShareIv, minerSettings);
                        break;
                    }

                    case MiningInstruction.DoNonceIvXor:
                    {
                        pocShareIv = DoNonceIvXorMiningInstruction(pocShareIv);
                        break;
                    }

                    case MiningInstruction.DoNonceIvEasySquareMath:
                    {
                        var result = NonceIvEasySquareMathUtility.DoNonceIvEasySquareMathMiningInstruction(blockTemplate, ref pocShareIv, previousFinalBlockTransactionHashKey);
                        if (!result) return false;
                        break;
                    }

                    case MiningInstruction.DoLz4CompressNonceIv:
                    {
                        pocShareIv = LZ4Codec.Wrap(pocShareIv, 0, pocShareIv.Length);
                        break;
                    }

                    case MiningInstruction.DoNonceIvIterations:
                    {
                        using var passwordDeriveBytes = new Rfc2898DeriveBytes(pocShareIv, algorithm.BlockchainMarkKey, minerSettings.PocShareNonceIvIteration);
                        pocShareIv = passwordDeriveBytes.GetBytes(16);
                        break;
                    }

                    case MiningInstruction.DoEncryptedPocShare:
                    {
                        rijndael.KeySize = 256;
                        rijndael.BlockSize = 128;
                        rijndael.Mode = CipherMode.CFB;
                        rijndael.Padding = PaddingMode.None;

                        using var cryptoTransform = rijndael.CreateEncryptor(previousFinalBlockTransactionHashKey, pocShareIv);

                        for (var i = 0; i < minerSettings.PowRoundAesShare; i++)
                        {
                            var packetLength = pocRandomData.Length;
                            var paddingSizeRequired = 16 - packetLength % 16;
                            var paddedLength = packetLength + paddingSizeRequired;
                            var paddedBytes = ArrayPool<byte>.Shared.Rent(paddedLength);

                            try
                            {
                                Buffer.BlockCopy(pocRandomData, 0, paddedBytes, 0, packetLength);

                                for (var j = 0; j < paddingSizeRequired; j++)
                                {
                                    paddedBytes[packetLength + j] = (byte) paddingSizeRequired;
                                }

                                pocRandomData = cryptoTransform.TransformFinalBlock(paddedBytes, 0, paddedLength);
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(paddedBytes);
                            }
                        }

                        break;
                    }
                }
            }

            var pocShare = Convert.ToHexString(pocRandomData);
            var shareDifficulty = BigInteger.Divide(BigInteger.Divide(ShaPowCalculation, blockTemplate.CurrentBlockDifficulty), BigInteger.Divide(new BigInteger(Sha3Utility.ComputeSha3512Hash(pocRandomData)), blockTemplate.CurrentBlockDifficulty));

            miningPowShare.WalletAddress = walletAddress;
            miningPowShare.BlockHeight = blockTemplate.CurrentBlockHeight;
            miningPowShare.BlockHash = blockTemplate.CurrentBlockHash;
            miningPowShare.Nonce = nonce;
            miningPowShare.PoWaCShare = pocShare;
            miningPowShare.PoWaCShareDifficulty = shareDifficulty;
            miningPowShare.NonceComputedHexString = Convert.ToHexString(pocShareIv);
            miningPowShare.Timestamp = timestamp;

            return true;
        }

        private static unsafe int GetRandomBetween(RandomNumberGenerator randomNumberGenerator, byte[] randomNumberBytes, int minimumValue, int maximumValue)
        {
            randomNumberGenerator.GetBytes(randomNumberBytes);

            fixed (byte* randomNumberPtr = randomNumberBytes)
            {
                var factor = maximumValue - minimumValue + 1;

                if (factor <= MaxFloatPrecision)
                {
                    return minimumValue + (int) (Math.Max(0, *randomNumberPtr / 255f - 0.0000001f) * factor);
                }

                return minimumValue + (int) (Math.Max(0, *randomNumberPtr / 255d - 0.00000000001) * factor);
            }
        }

        private static void DoNonceIvMiningInstruction(ref byte[] pocShareIv, MiningSettings minerSettings)
        {
            if (pocShareIv.Length == Sha3Utility.Sha3512OutputSize)
            {
                for (var i = 0; i < minerSettings.PocRoundShaNonce; i++)
                {
                    Sha3Utility.ComputeSha3512Hash(pocShareIv, pocShareIv);
                }
            }
            else
            {
                pocShareIv = Sha3Utility.ComputeSha3512Hash(pocShareIv);

                for (var i = 1; i < minerSettings.PocRoundShaNonce; i++)
                {
                    Sha3Utility.ComputeSha3512Hash(pocShareIv, pocShareIv);
                }
            }
        }

        private static byte[] DoNonceIvXorMiningInstruction(byte[] pocShareIv)
        {
            var pocShareIvLength = pocShareIv.Length;
            var pocShareIvMath = new byte[pocShareIvLength];

            if (!Vector.IsHardwareAccelerated)
            {
                var vectorSize = Vector<byte>.Count;
                var iterationLength = pocShareIvLength - vectorSize;
                var i = 0;

                var pocShareIvSpan = pocShareIv.AsSpan();
                Span<byte> pocShareIvReverseSpan = stackalloc byte[pocShareIvLength];
                pocShareIvSpan.CopyTo(pocShareIvReverseSpan);
                pocShareIvReverseSpan.Reverse();

                for (; i <= iterationLength; i += vectorSize)
                {
                    var test = new Vector<byte>(pocShareIvSpan.Slice(i, vectorSize));
                    var test2 = new Vector<byte>(pocShareIvReverseSpan.Slice(i, vectorSize));
                    Vector.Xor(test, test2).CopyTo(pocShareIvMath, i);
                }

                for (; i < pocShareIvLength; i++)
                {
                    pocShareIvMath[i] = (byte) (pocShareIvSpan[i] ^ pocShareIvReverseSpan[i]);
                }
            }
            else
            {
                for (var i = 0; i < pocShareIvLength; i++)
                {
                    pocShareIvMath[i] = (byte) (pocShareIv[i] ^ pocShareIv[^(1 + i)]);
                }
            }

            return pocShareIvMath;
        }
    }
}