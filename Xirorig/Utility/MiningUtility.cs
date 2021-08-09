using System;
using System.Buffers;
using System.Numerics;
using System.Security.Cryptography;
using LZ4;
using Org.BouncyCastle.Crypto.Digests;
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

        private static readonly Func<byte[], byte[]> DoNonceIvXorMiningInstructions;
        private static readonly BigInteger ShaPowCalculation = BigInteger.Pow(2, 512);

        static MiningUtility()
        {
            DoNonceIvXorMiningInstructions = Vector.IsHardwareAccelerated ? NonceIvXorMiningInstructionsVectorized : NonceIvXorMiningInstructions;
        }

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

        public static bool DoPowShare(MiningPowShare miningPowShare, BlockTemplate blockTemplate, Sha3Digest sha3Digest, RijndaelManaged rijndaelManaged, IAlgorithm algorithm, string walletAddress, long nonce, long timestamp, byte[] pocRandomData, byte[] previousFinalBlockTransactionHashKey)
        {
            var pocShareIv = BitConverter.GetBytes(nonce);

            var minerSettings = blockTemplate.MiningSettings;
            var miningInstructions = minerSettings.MiningInstructions;
            var doNonceIvXorMiningInstructions = DoNonceIvXorMiningInstructions;

            foreach (var miningInstruction in miningInstructions)
            {
                switch (miningInstruction)
                {
                    case MiningInstruction.DoNonceIv:
                    {
                        if (pocShareIv.Length == Sha3Utility.Sha3512OutputSize)
                        {
                            for (var i = 0; i < minerSettings.PocRoundShaNonce; i++)
                            {
                                Sha3Utility.DoSha3512Hash(sha3Digest, pocShareIv, pocShareIv);
                            }
                        }
                        else
                        {
                            pocShareIv = Sha3Utility.DoSha3512Hash(sha3Digest, pocShareIv);

                            for (var i = 0; i < minerSettings.PocRoundShaNonce - 1; i++)
                            {
                                Sha3Utility.DoSha3512Hash(sha3Digest, pocShareIv, pocShareIv);
                            }
                        }

                        break;
                    }

                    case MiningInstruction.DoNonceIvXor:
                    {
                        pocShareIv = doNonceIvXorMiningInstructions(pocShareIv);
                        break;
                    }

                    case MiningInstruction.DoNonceIvEasySquareMath:
                    {
                        var totalRetry = 0;
                        var arrayPool = ArrayPool<byte>.Shared;

                        var blockDifficultyBytes = blockTemplate.CurrentBlockDifficulty.ToByteArray();
                        var newNonceGenerated = false;
                        var newNonce = 0L;

                        while (totalRetry < minerSettings.PocShareNonceMaxSquareRetry)
                        {
                            var pocShareWorkToDoBytes = arrayPool.Rent(pocShareIv.Length + previousFinalBlockTransactionHashKey.Length + 8 + blockDifficultyBytes.Length);

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

                                Sha3Utility.DoSha3512Hash(sha3Digest, pocShareWorkToDoBytes, pocShareWorkToDoBytes);

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

                                pocShareIv = Sha3Utility.DoSha3512Hash(sha3Digest, pocShareIv);

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
                                pocShareIv = Sha3Utility.DoSha3512Hash(sha3Digest, pocShareIv);
                            }

                            newNonce = pocShareIv[0] + (pocShareIv[1] << 8) + (pocShareIv[2] << 16) + ((uint) pocShareIv[3] << 24);
                        }

                        if (newNonce >= minerSettings.PocShareNonceMin && newNonce <= minerSettings.PocShareNonceMax)
                        {
                            Array.Resize(ref pocShareIv, 8);

                            pocShareIv[0] = (byte) (newNonce & 0xFF);
                            pocShareIv[1] = (byte) ((newNonce & 0xFF_00) >> 8);
                            pocShareIv[2] = (byte) ((newNonce & 0xFF_00_00) >> 16);
                            pocShareIv[3] = (byte) ((newNonce & 0xFF_00_00_00) >> 24);
                            pocShareIv[4] = (byte) ((newNonce & 0xFF_00_00_00_00) >> 32);
                            pocShareIv[5] = (byte) ((newNonce & 0xFF_00_00_00_00_00) >> 40);
                            pocShareIv[6] = (byte) ((newNonce & 0xFF_00_00_00_00_00_00) >> 48);
                            pocShareIv[7] = (byte) ((newNonce & 0x7F_00_00_00_00_00_00_00) >> 56);
                        }
                        else
                        {
                            return false;
                        }

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
                        rijndaelManaged.KeySize = 256;
                        rijndaelManaged.BlockSize = 128;
                        rijndaelManaged.Key = previousFinalBlockTransactionHashKey;
                        rijndaelManaged.IV = pocShareIv;
                        rijndaelManaged.Mode = CipherMode.CFB;
                        rijndaelManaged.Padding = PaddingMode.None;

                        using var cryptoTransform = rijndaelManaged.CreateEncryptor(previousFinalBlockTransactionHashKey, pocShareIv);

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
            var shareDifficulty = BigInteger.Divide(BigInteger.Divide(ShaPowCalculation, blockTemplate.CurrentBlockDifficulty), BigInteger.Divide(new BigInteger(Sha3Utility.DoSha3512Hash(sha3Digest, pocRandomData)), blockTemplate.CurrentBlockDifficulty));

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

        private static byte[] NonceIvXorMiningInstructions(byte[] pocShareIv)
        {
            var pocShareIvLength = pocShareIv.Length;
            var pocShareIvMath = new byte[pocShareIvLength];

            for (var i = 0; i < pocShareIvLength; i++)
            {
                pocShareIvMath[i] = (byte) (pocShareIv[i] ^ pocShareIv[^(1 + i)]);
            }

            return pocShareIvMath;
        }

        private static unsafe byte[] NonceIvXorMiningInstructionsVectorized(byte[] pocShareIv)
        {
            var pocShareIvLength = pocShareIv.Length;
            var pocShareIvMath = new byte[pocShareIvLength];

            var vectorSize = Vector<byte>.Count;
            var i = 0;

            var pocShareIvSpan = pocShareIv.AsSpan();
            Span<byte> pocShareIvReverseSpan = stackalloc byte[pocShareIvLength];
            pocShareIvSpan.CopyTo(pocShareIvReverseSpan);
            pocShareIvReverseSpan.Reverse();

            if (pocShareIvLength >= vectorSize)
            {
                var iterationSize = pocShareIvLength / vectorSize * vectorSize;

                for (; i < iterationSize; i += vectorSize)
                {
                    var test = new Vector<byte>(pocShareIvSpan.Slice(i, vectorSize));
                    var test2 = new Vector<byte>(pocShareIvReverseSpan.Slice(i, vectorSize));
                    Vector.Xor(test, test2).CopyTo(pocShareIvMath, i);
                }
            }

            for (; i < pocShareIvLength; i++)
            {
                pocShareIvMath[i] = (byte) (pocShareIvSpan[i] ^ pocShareIvReverseSpan[i]);
            }

            return pocShareIvMath;
        }
    }
}