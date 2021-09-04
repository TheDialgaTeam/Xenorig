using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using LZ4;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using Xirorig.Algorithm.Xiropht.Decentralized.Api.JobResult.Models;
using Xirorig.Algorithm.Xiropht.Decentralized.Api.JobTemplate.Models;
using Xirorig.Network.Api.JobTemplate;
using Xirorig.Options;
using Xirorig.Utility;
using CpuMiner = Xirorig.Miner.Backend.CpuMiner;

namespace Xirorig.Algorithm.Xiropht.Decentralized
{
    internal class XirophtDecentralizedCpuMiner : CpuMiner
    {
        private static readonly BigInteger ShaPowCalculation = BigInteger.Pow(2, 512);

        private static bool _isGeneratePocRandomDataAvailable = true;
        private static bool _isUpdatePocRandomDataAvailable = true;
        private static bool _isNonceIvEasySquareMathAvailable = true;
        private static bool _isLz4CompressNonceIvAvailable = true;
        private static bool _isNonceIvIterationsAvailable = true;
        private static bool _isEncryptedPocShareAvailable = true;

        private readonly byte[] _blockchainMarkKey;
        private readonly string _walletAddress;
        private readonly byte[] _walletAddressBytes;

        private byte[] _pocRandomData = Array.Empty<byte>();

        public XirophtDecentralizedCpuMiner(int threadId, CpuMinerThreadConfiguration threadConfiguration, CancellationToken cancellationToken, Pool pool) : base(threadId, threadConfiguration, cancellationToken)
        {
            var coin = pool.GetCoin();

            if (coin.Equals("xiropht", StringComparison.OrdinalIgnoreCase))
            {
                var networkBytes = new byte[] { 0x73, 0x61, 0x6d, 0x20, 0x73, 0x65, 0x67, 0x75, 0x72, 0x61 };
                _blockchainMarkKey = Encoding.ASCII.GetBytes(Convert.ToHexString(Sha3Utility.ComputeSha512Hash(networkBytes)));
            }
            else if (coin.Equals("xirobod", StringComparison.OrdinalIgnoreCase))
            {
                var networkBytes = new byte[] { 0x73, 0x61, 0x6d, 0x20, 0x73, 0x65, 0x67, 0x75, 0x72, 0x61 };
                _blockchainMarkKey = Encoding.ASCII.GetBytes(Convert.ToHexString(Sha3Utility.ComputeSha512Hash(networkBytes)));
            }
            else
            {
                throw new JsonException("The selected coin is not supported.");
            }

            _walletAddress = pool.GetUsername();
            _walletAddressBytes = Base58Utility.DecodeWithoutChecksum(pool.GetUsername());
        }

        [DllImport("xirorig_native")]
        private static extern void XirophtDecentralizedCpuMiner_GeneratePocRandomData(byte[] pocRandomData, int randomNumber, int randomNumber2, long timestamp, ulong randomDataShareChecksumSize, byte[] walletAddress, ulong walletAddressSize, long currentBlockHeight, long nonce);

        [DllImport("xirorig_native")]
        private static extern void XirophtDecentralizedCpuMiner_UpdatePocRandomData(byte[] pocRandomData, long timestamp, ulong randomDataShareChecksumSize, ulong walletAddressSize, long nonce);

        [DllImport("xirorig_native")]
        private static extern int XirophtDecentralizedCpuMiner_DoNonceIvEasySquareMathMiningInstruction(int pocShareNonceMaxSquareRetry, int pocShareNonceNoSquareFoundShaRounds, long pocShareNonceMin, long pocShareNonceMax, long currentBlockHeight, byte[] pocShareIv, int pocShareIvLength, byte[] previousFinalBlockTransactionHashKey, int previousFinalBlockTransactionHashKeyLength, byte[] blockDifficulty, int blockDifficultyLength);

        [DllImport("xirorig_native")]
        private static extern int XirophtDecentralizedCpuMiner_GetMaxLz4CompressSize(int inputSize);

        [DllImport("xirorig_native")]
        private static extern unsafe int XirophtDecentralizedCpuMiner_DoLz4CompressNonceIvMiningInstruction(byte[] input, int inputSize, byte* output);

        [DllImport("xirorig_native")]
        private static extern int XirophtDecentralizedCpuMiner_DoNonceIvIterationsMiningInstruction(byte[] password, int passwordLength, byte[] salt, int saltLength, int iterations, int keyLength, byte[] output);

        [DllImport("xirorig_native")]
        private static extern int XirophtDecentralizedCpuMiner_GetAes256Cfb128OutputSize(int iterations, int dataLength);

        [DllImport("xirorig_native")]
        private static extern int XirophtDecentralizedCpuMiner_DoEncryptedPocShareMiningInstruction(byte[] key, byte[] iv, int iterations, byte[] data, int dataLength, byte[] output);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GeneratePocRandomData(BlockTemplate blockTemplate, byte[] pocRandomData, byte[] walletAddress, long nonce, long timestamp)
        {
            if (!_isGeneratePocRandomDataAvailable)
            {
                SoftwareGeneratePocRandomData(blockTemplate, pocRandomData, walletAddress, nonce, timestamp);
                return;
            }

            try
            {
                var minerSettings = blockTemplate.MiningSettings;
                var randomDataShareChecksum = minerSettings.RandomDataShareChecksum;

                RandomNumberGenerator.Fill(pocRandomData.AsSpan(16, randomDataShareChecksum));

                var previousBlockTransactionCount = blockTemplate.PreviousBlockTransactionCount;
                var randomNumber = RandomNumberGeneratorUtility.GetRandomBetween(0, previousBlockTransactionCount);
                var randomNumber2 = previousBlockTransactionCount - randomNumber;

                XirophtDecentralizedCpuMiner_GeneratePocRandomData(pocRandomData, randomNumber, randomNumber2, timestamp, (ulong) randomDataShareChecksum, walletAddress, (ulong) minerSettings.WalletAddressDataSize, blockTemplate.CurrentBlockHeight, nonce);
            }
            catch (Exception)
            {
                _isGeneratePocRandomDataAvailable = false;
                SoftwareGeneratePocRandomData(blockTemplate, pocRandomData, walletAddress, nonce, timestamp);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareGeneratePocRandomData(BlockTemplate blockTemplate, byte[] pocRandomData, byte[] walletAddress, long nonce, long timestamp)
        {
            // Random numbers must be between 0 to previousBlockTransactionCount.
            var previousBlockTransactionCount = blockTemplate.PreviousBlockTransactionCount;
            var randomNumber = RandomNumberGeneratorUtility.GetRandomBetween(0, previousBlockTransactionCount);
            var randomNumber2 = previousBlockTransactionCount - randomNumber;

            // randomNumber
            Unsafe.As<byte, int>(ref pocRandomData[0]) = randomNumber;

            // randomNumber2
            Unsafe.As<byte, int>(ref pocRandomData[4]) = randomNumber2;

            // timestamp
            Unsafe.As<byte, long>(ref pocRandomData[8]) = timestamp;

            var minerSettings = blockTemplate.MiningSettings;
            var randomDataShareChecksum = minerSettings.RandomDataShareChecksum;
            var walletAddressDataSize = minerSettings.WalletAddressDataSize;

            RandomNumberGenerator.Fill(pocRandomData.AsSpan(16, randomDataShareChecksum));

            var walletAddressOffset = 16 + randomDataShareChecksum;
            Buffer.BlockCopy(walletAddress, 0, pocRandomData, walletAddressOffset, walletAddressDataSize);

            var blockHeightOffset = walletAddressOffset + walletAddressDataSize;

            // block height
            Unsafe.As<byte, long>(ref pocRandomData[blockHeightOffset]) = blockTemplate.CurrentBlockHeight;

            // nonce
            Unsafe.As<byte, long>(ref pocRandomData[blockHeightOffset + 8]) = nonce;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdatePocRandomData(BlockTemplate blockTemplate, byte[] pocRandomData, long timestamp, long nonce)
        {
            if (!_isUpdatePocRandomDataAvailable)
            {
                SoftwareUpdatePocRandomData(blockTemplate, pocRandomData, timestamp, nonce);
            }

            try
            {
                var minerSettings = blockTemplate.MiningSettings;
                XirophtDecentralizedCpuMiner_UpdatePocRandomData(pocRandomData, timestamp, (ulong) minerSettings.RandomDataShareChecksum, (ulong) minerSettings.WalletAddressDataSize, nonce);
            }
            catch (Exception)
            {
                _isUpdatePocRandomDataAvailable = false;
                SoftwareUpdatePocRandomData(blockTemplate, pocRandomData, timestamp, nonce);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareUpdatePocRandomData(BlockTemplate blockTemplate, byte[] pocRandomData, long timestamp, long nonce)
        {
            var minerSettings = blockTemplate.MiningSettings;

            // timestamp
            Unsafe.As<byte, long>(ref pocRandomData[8]) = timestamp;

            var offset = 16 + minerSettings.RandomDataShareChecksum + minerSettings.WalletAddressDataSize + 8;

            // nonce
            Unsafe.As<byte, long>(ref pocRandomData[offset]) = nonce;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DoPowShare(BlockTemplate blockTemplate, long nonce, byte[] previousFinalBlockTransactionHashKey, byte[] blockchainMarkKey, byte[] pocRandomData, MiningPowShare miningPowShare, string walletAddress, long timestamp)
        {
            var pocShareIv = BitConverter.GetBytes(nonce);

            var minerSettings = blockTemplate.MiningSettings;
            var miningInstructions = minerSettings.MiningInstructions;

            foreach (var miningInstruction in miningInstructions)
            {
                switch (miningInstruction)
                {
                    case MiningInstruction.DoNonceIv:
                        DoNonceIvMiningInstruction(ref pocShareIv, minerSettings);
                        break;

                    case MiningInstruction.DoNonceIvXor:
                        pocShareIv = DoNonceIvXorMiningInstruction(pocShareIv);
                        break;

                    case MiningInstruction.DoNonceIvEasySquareMath:
                        if (!DoNonceIvEasySquareMathMiningInstruction(blockTemplate, ref pocShareIv, previousFinalBlockTransactionHashKey)) return false;
                        break;

                    case MiningInstruction.DoLz4CompressNonceIv:
                        pocShareIv = DoLz4CompressNonceIvMiningInstruction(pocShareIv);
                        break;

                    case MiningInstruction.DoNonceIvIterations:
                        pocShareIv = DoNonceIvIterationsMiningInstruction(pocShareIv, blockchainMarkKey, minerSettings.PocShareNonceIvIteration, 16);
                        break;

                    case MiningInstruction.DoEncryptedPocShare:
                        DoEncryptedPocShare(previousFinalBlockTransactionHashKey, pocShareIv, minerSettings.PowRoundAesShare, ref pocRandomData);
                        break;
                }
            }

            var pocShare = Convert.ToHexString(pocRandomData);
            var shareDifficulty = BigInteger.Divide(BigInteger.Divide(ShaPowCalculation, blockTemplate.CurrentBlockDifficulty), BigInteger.Divide(new BigInteger(Sha3Utility.ComputeSha512Hash(pocRandomData)), blockTemplate.CurrentBlockDifficulty));

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoNonceIvMiningInstruction(ref byte[] pocShareIv, MiningSettings minerSettings)
        {
            if (pocShareIv.Length == Sha3Utility.Sha512OutputSize)
            {
                for (var i = 0; i < minerSettings.PocRoundShaNonce; i++)
                {
                    Sha3Utility.ComputeSha512Hash(pocShareIv, pocShareIv);
                }
            }
            else
            {
                pocShareIv = Sha3Utility.ComputeSha512Hash(pocShareIv);

                for (var i = 1; i < minerSettings.PocRoundShaNonce; i++)
                {
                    Sha3Utility.ComputeSha512Hash(pocShareIv, pocShareIv);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DoNonceIvEasySquareMathMiningInstruction(BlockTemplate blockTemplate, ref byte[] pocShareIv, byte[] previousFinalBlockTransactionHashKey)
        {
            if (!_isNonceIvEasySquareMathAvailable) return SoftwareDoNonceIvEasySquareMathMiningInstruction(blockTemplate, ref pocShareIv, previousFinalBlockTransactionHashKey);

            try
            {
                var minerSettings = blockTemplate.MiningSettings;
                var blockDifficultyBytes = blockTemplate.CurrentBlockDifficulty.ToByteArray();

                var result = XirophtDecentralizedCpuMiner_DoNonceIvEasySquareMathMiningInstruction(
                    minerSettings.PocShareNonceMaxSquareRetry,
                    minerSettings.PocShareNonceNoSquareFoundShaRounds,
                    minerSettings.PocShareNonceMin,
                    minerSettings.PocShareNonceMax,
                    blockTemplate.CurrentBlockHeight,
                    pocShareIv,
                    pocShareIv.Length,
                    previousFinalBlockTransactionHashKey,
                    previousFinalBlockTransactionHashKey.Length,
                    blockDifficultyBytes,
                    blockDifficultyBytes.Length
                );

                if (result == 0) return false;

                Array.Resize(ref pocShareIv, 8);

                return true;
            }
            catch (Exception)
            {
                _isNonceIvEasySquareMathAvailable = false;
                return SoftwareDoNonceIvEasySquareMathMiningInstruction(blockTemplate, ref pocShareIv, previousFinalBlockTransactionHashKey);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SoftwareDoNonceIvEasySquareMathMiningInstruction(BlockTemplate blockTemplate, ref byte[] pocShareIv, byte[] previousFinalBlockTransactionHashKey)
        {
            var totalRetry = 0;

            var blockDifficultyBytes = blockTemplate.CurrentBlockDifficulty.ToByteArray();
            var minerSettings = blockTemplate.MiningSettings;

            var newNonceGenerated = false;
            var newNonce = 0L;

            var minimumLength = pocShareIv.Length + previousFinalBlockTransactionHashKey.Length + 8 + blockDifficultyBytes.Length;
            var pocShareWorkToDoBytes = ArrayPool<byte>.Shared.Rent(minimumLength);

            try
            {
                while (totalRetry < minerSettings.PocShareNonceMaxSquareRetry)
                {
                    Buffer.BlockCopy(pocShareIv, 0, pocShareWorkToDoBytes, 0, pocShareIv.Length);
                    Buffer.BlockCopy(blockDifficultyBytes, 0, pocShareWorkToDoBytes, pocShareIv.Length, blockDifficultyBytes.Length);

                    var offset = pocShareIv.Length + blockDifficultyBytes.Length;

                    // Block Height
                    Unsafe.As<byte, long>(ref pocShareWorkToDoBytes[offset]) = blockTemplate.CurrentBlockHeight;

                    Buffer.BlockCopy(previousFinalBlockTransactionHashKey, 0, pocShareWorkToDoBytes, offset + 8, previousFinalBlockTransactionHashKey.Length);

                    Sha3Utility.ComputeSha512Hash(pocShareWorkToDoBytes, 0, minimumLength, pocShareWorkToDoBytes);

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
                            newNonce = (byte) (pocShareWorkToDoBytes[i] + pocShareWorkToDoBytes[i]) + ((byte) (pocShareWorkToDoBytes[i + 2] + pocShareWorkToDoBytes[i + 2]) << 8) + ((byte) (pocShareWorkToDoBytes[i + 4] + pocShareWorkToDoBytes[i + 4]) << 16) + ((long) (byte) (pocShareWorkToDoBytes[i + 6] + pocShareWorkToDoBytes[i + 6]) << 24);
                            newNonceGenerated = true;
                            break;
                        }
                    }

                    if (newNonceGenerated) break;

                    pocShareIv = Sha3Utility.ComputeSha512Hash(pocShareIv);
                    totalRetry++;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pocShareWorkToDoBytes);
            }

            if (!newNonceGenerated)
            {
                for (var i = 0; i < minerSettings.PocShareNonceNoSquareFoundShaRounds; i++)
                {
                    pocShareIv = Sha3Utility.ComputeSha512Hash(pocShareIv);
                }

                newNonce = pocShareIv[0] + (pocShareIv[1] << 8) + (pocShareIv[2] << 16) + ((long) pocShareIv[3] << 24);
            }

            if (newNonce < minerSettings.PocShareNonceMin || newNonce > minerSettings.PocShareNonceMax) return false;

            Array.Resize(ref pocShareIv, 8);
            
            Unsafe.As<byte, long>(ref pocShareIv[0]) = newNonce;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe byte[] DoLz4CompressNonceIvMiningInstruction(byte[] pocShareIv)
        {
            if (!_isLz4CompressNonceIvAvailable) return SoftwareDoLz4CompressNonceIvMiningInstruction(pocShareIv);

            try
            {
                var pocShareIvLength = pocShareIv.Length;
                Span<byte> output = stackalloc byte[XirophtDecentralizedCpuMiner_GetMaxLz4CompressSize(pocShareIvLength) + 8];

                fixed (byte* outputPtr = output)
                {
                    var size = XirophtDecentralizedCpuMiner_DoLz4CompressNonceIvMiningInstruction(pocShareIv, pocShareIvLength, outputPtr);
                    return output[..size].ToArray();
                }
            }
            catch (Exception)
            {
                _isLz4CompressNonceIvAvailable = false;
                return SoftwareDoLz4CompressNonceIvMiningInstruction(pocShareIv);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] SoftwareDoLz4CompressNonceIvMiningInstruction(byte[] pocShareIv)
        {
            return LZ4Codec.Wrap(pocShareIv, 0, pocShareIv.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] DoNonceIvIterationsMiningInstruction(byte[] password, byte[] salt, int iterations, int keyLength)
        {
            if (!_isNonceIvIterationsAvailable) return SoftwareDoNonceIvIterationsMiningInstruction(password, salt, iterations, keyLength);

            try
            {
                var output = new byte[16];

                XirophtDecentralizedCpuMiner_DoNonceIvIterationsMiningInstruction(password, password.Length, salt, salt.Length, iterations, keyLength, output);

                return output;
            }
            catch (Exception)
            {
                _isNonceIvIterationsAvailable = false;
                return SoftwareDoNonceIvIterationsMiningInstruction(password, salt, iterations, keyLength);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] SoftwareDoNonceIvIterationsMiningInstruction(byte[] password, byte[] salt, int iterations, int keyLength)
        {
            using var passwordDeriveBytes = new Rfc2898DeriveBytes(password, salt, iterations);
            return passwordDeriveBytes.GetBytes(keyLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoEncryptedPocShare(byte[] key, byte[] iv, int iterations, ref byte[] data)
        {
            if (!_isEncryptedPocShareAvailable)
            {
                SoftwareDoEncryptedPocShare(key, iv, iterations, ref data);
                return;
            }

            try
            {
                var outputSize = XirophtDecentralizedCpuMiner_GetAes256Cfb128OutputSize(iterations, data.Length);
                var output = new byte[outputSize];

                XirophtDecentralizedCpuMiner_DoEncryptedPocShareMiningInstruction(key, iv, iterations, data, data.Length, output);

                data = output;
            }
            catch (Exception)
            {
                _isEncryptedPocShareAvailable = false;
                SoftwareDoEncryptedPocShare(key, iv, iterations, ref data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareDoEncryptedPocShare(byte[] key, byte[] iv, int iterations, ref byte[] data)
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CFB;
            aes.Padding = PaddingMode.None;
            aes.FeedbackSize = 128;

            using var cryptoTransform = aes.CreateEncryptor(key, iv);

            for (var i = 0; i < iterations; i++)
            {
                var packetLength = data.Length;
                var paddingSizeRequired = 16 - packetLength % 16;
                var paddedLength = packetLength + paddingSizeRequired;
                var paddedBytes = ArrayPool<byte>.Shared.Rent(paddedLength);

                try
                {
                    Buffer.BlockCopy(data, 0, paddedBytes, 0, packetLength);

                    for (var j = 0; j < paddingSizeRequired; j++)
                    {
                        paddedBytes[packetLength + j] = (byte) paddingSizeRequired;
                    }

                    data = cryptoTransform.TransformFinalBlock(paddedBytes, 0, paddedLength);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(paddedBytes);
                }
            }
        }

        protected override void ExecuteJob(IJobTemplate jobTemplate, CancellationToken cancellationToken)
        {
            if (jobTemplate is not BlockTemplate blockTemplate) return;

            var miningSettings = blockTemplate.MiningSettings;

            if (_pocRandomData.Length != miningSettings.RandomDataShareSize)
            {
                _pocRandomData = new byte[miningSettings.RandomDataShareSize];
            }

            var previousBlockFinalTransactionHashBytes = Encoding.ASCII.GetBytes(blockTemplate.PreviousBlockFinalTransactionHash);
            previousBlockFinalTransactionHashBytes = Sha3Utility.ComputeSha512Hash(previousBlockFinalTransactionHashBytes);
            Array.Resize(ref previousBlockFinalTransactionHashBytes, 32);

            var minNonce = miningSettings.PocShareNonceMin;
            var maxNonce = miningSettings.PocShareNonceMax;

            var currentNonce = minNonce;

            var miningPowShare = new MiningPowShare();

            LogCurrentJob($"{AnsiEscapeCodeConstants.BlueForegroundColor}Thread: {{ThreadId}} | Job Difficulty: {{JobDifficulty:l}}{AnsiEscapeCodeConstants.Reset}", ThreadId, blockTemplate.CurrentBlockDifficulty.ToString());

            while (!cancellationToken.IsCancellationRequested)
            {
                var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

                if (currentNonce == minNonce)
                {
                    GeneratePocRandomData(blockTemplate, _pocRandomData, _walletAddressBytes, currentNonce, timestamp);
                }
                else
                {
                    UpdatePocRandomData(blockTemplate, _pocRandomData, timestamp, currentNonce);
                }

                if (DoPowShare(blockTemplate, currentNonce, previousBlockFinalTransactionHashBytes, _blockchainMarkKey, _pocRandomData, miningPowShare, _walletAddress, timestamp))
                {
                    IncrementHashCalculated();

                    if (miningPowShare.PoWaCShareDifficulty >= blockTemplate.CurrentBlockDifficulty)
                    {
                        LogCurrentJob($"{AnsiEscapeCodeConstants.GreenForegroundColor}Thread: {{ThreadId}} | Block Found | Nonce: {{Nonce}} | Diff: {{ShareDifficulty:l}}{AnsiEscapeCodeConstants.Reset}", ThreadId, currentNonce, miningPowShare.PoWaCShareDifficulty.ToString());
                        SubmitJobResult(blockTemplate, new MiningShare(miningPowShare, timestamp));
                        break;
                    }
                }

                if (++currentNonce > maxNonce)
                {
                    currentNonce = minNonce;
                }
            }
        }
    }
}