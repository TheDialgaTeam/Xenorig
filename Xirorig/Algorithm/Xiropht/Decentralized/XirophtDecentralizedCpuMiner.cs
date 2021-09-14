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
using Xirorig.Utilities;
using CpuMiner = Xirorig.Miner.Backend.CpuMiner;

namespace Xirorig.Algorithm.Xiropht.Decentralized
{
    internal class XirophtDecentralizedCpuMiner : CpuMiner
    {
        private readonly ref struct XirophtDataBuffer
        {
            public Span<byte> PocRandomData { get; }

            public Span<byte> PocShareIv { get; }

            public Span<byte> PocShareWorkToDoBytes { get; }

            public ReadOnlySpan<byte> CurrentBlockDifficulty { get; }

            public ReadOnlySpan<byte> PreviousBlockFinalTransactionHash { get; }

            public XirophtDataBuffer(Span<byte> buffer, int pocRandomDataSize, int pocShareIvSize, int pocShareWorkToDoBytesSize, BigInteger currentBlockDifficulty, string previousBlockFinalTransactionHash)
            {
                PocRandomData = buffer[..pocRandomDataSize];
                PocShareIv = buffer.Slice(pocRandomDataSize, pocShareIvSize);
                PocShareWorkToDoBytes = buffer.Slice(pocRandomDataSize + pocShareIvSize, pocShareWorkToDoBytesSize);

                var currentBlockDifficultyOffset = pocRandomDataSize + pocShareIvSize + pocShareWorkToDoBytesSize;
                currentBlockDifficulty.TryWriteBytes(buffer[currentBlockDifficultyOffset..], out var currentBlockDifficultySize);
                CurrentBlockDifficulty = buffer.Slice(currentBlockDifficultyOffset, currentBlockDifficultySize);

                var previousBlockFinalTransactionHashOffset = currentBlockDifficultyOffset + currentBlockDifficultySize;
                var previousBlockFinalTransactionHashSize = Encoding.ASCII.GetBytes(previousBlockFinalTransactionHash, buffer[previousBlockFinalTransactionHashOffset..]);
                Sha3Utility.TryComputeSha512Hash(buffer.Slice(previousBlockFinalTransactionHashOffset, previousBlockFinalTransactionHashSize), buffer[previousBlockFinalTransactionHashOffset..], out var _);
                PreviousBlockFinalTransactionHash = buffer.Slice(previousBlockFinalTransactionHashOffset, 32);
            }
        }

        private static class Native
        {
            [DllImport(Program.XirorigNativeLibrary)]
            public static extern void XirophtDecentralizedCpuMiner_GeneratePocRandomData(in byte pocRandomData, int randomNumber, int randomNumber2, long timestamp, int randomDataShareChecksumSize, in byte walletAddress, int walletAddressSize, long currentBlockHeight, long nonce);

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern void XirophtDecentralizedCpuMiner_UpdatePocRandomData(in byte pocRandomData, long timestamp, int randomDataShareChecksumSize, int walletAddressSize, long nonce);

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern void XirophtDecentralizedCpuMiner_DoNonceIvMiningInstruction(in byte pocShareIv, ref int pocShareIvSize, int pocRoundShaNonce);

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern void XirophtDecentralizedCpuMiner_DoNonceIvXorMiningInstruction(in byte pocShareIv, int pocShareIvSize);

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern int XirophtDecentralizedCpuMiner_DoNonceIvEasySquareMathMiningInstruction(int pocShareNonceMaxSquareRetry, int pocShareNonceNoSquareFoundShaRounds, long pocShareNonceMin, long pocShareNonceMax, long currentBlockHeight, in byte pocShareIv, ref int pocShareIvLength, in byte pocShareWorkToDoBytes, in byte blockDifficulty, int blockDifficultyLength, in byte previousFinalBlockTransactionHashKey, int previousFinalBlockTransactionHashKeyLength);

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern void XirophtDecentralizedCpuMiner_DoLz4CompressNonceIvMiningInstruction(in byte input, ref int inputSize);

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern int XirophtDecentralizedCpuMiner_DoNonceIvIterationsMiningInstruction(in byte pocShareIv, ref int pocShareIvSize, in byte blockchainMarkKey, int blockchainMarkKeySize, int pocShareNonceIvIteration, int keyLength);

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern int XirophtDecentralizedCpuMiner_DoEncryptedPocShareMiningInstruction(in byte key, in byte iv, int iterations, in byte data, ref int dataLength);
        }

        private const int MaxStackSize = 1024;

        private static readonly BigInteger ShaPowCalculation = BigInteger.Pow(2, 512);

        private static bool _isGeneratePocRandomDataAvailable = true;
        private static bool _isUpdatePocRandomDataAvailable = true;
        private static bool _isNonceIvAvailable = true;
        private static bool _isNonceIvXorAvailable = true;
        private static bool _isNonceIvEasySquareMathAvailable = true;
        private static bool _isLz4CompressNonceIvAvailable = true;
        private static bool _isNonceIvIterationsAvailable = true;
        private static bool _isEncryptedPocShareAvailable = true;

        private readonly byte[] _blockchainMarkKey;

        private readonly string _walletAddress;
        private readonly byte[] _walletAddressBytes;

        private byte[] _heapDataBuffer = Array.Empty<byte>();

        public XirophtDecentralizedCpuMiner(int threadId, CpuMinerThreadConfiguration threadConfiguration, Pool pool, CancellationToken cancellationToken) : base(threadId, threadConfiguration, cancellationToken)
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
            _walletAddressBytes = Base58Utility.Decode(pool.GetUsername(), false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GeneratePocRandomData(BlockTemplate blockTemplate, Span<byte> pocRandomData, ReadOnlySpan<byte> walletAddress, long nonce, long timestamp)
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

                RandomNumberGenerator.Fill(pocRandomData.Slice(16, randomDataShareChecksum));

                var previousBlockTransactionCount = blockTemplate.PreviousBlockTransactionCount;
                var randomNumber = RandomNumberGeneratorUtility.GetRandomBetween(0, previousBlockTransactionCount);
                var randomNumber2 = previousBlockTransactionCount - randomNumber;

                Native.XirophtDecentralizedCpuMiner_GeneratePocRandomData(MemoryMarshal.GetReference(pocRandomData), randomNumber, randomNumber2, timestamp, randomDataShareChecksum, MemoryMarshal.GetReference(walletAddress), minerSettings.WalletAddressDataSize, blockTemplate.CurrentBlockHeight, nonce);
            }
            catch (Exception)
            {
                _isGeneratePocRandomDataAvailable = false;
                SoftwareGeneratePocRandomData(blockTemplate, pocRandomData, walletAddress, nonce, timestamp);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareGeneratePocRandomData(BlockTemplate blockTemplate, Span<byte> pocRandomData, ReadOnlySpan<byte> walletAddress, long nonce, long timestamp)
        {
            // Random numbers must be between 0 to previousBlockTransactionCount.
            var previousBlockTransactionCount = blockTemplate.PreviousBlockTransactionCount;
            var randomNumber = RandomNumberGeneratorUtility.GetRandomBetween(0, previousBlockTransactionCount);
            var randomNumber2 = previousBlockTransactionCount - randomNumber;

            // randomNumber
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(pocRandomData), randomNumber);

            // randomNumber2
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(pocRandomData), 4), randomNumber2);

            // timestamp
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(pocRandomData), 8), timestamp);

            var minerSettings = blockTemplate.MiningSettings;
            var randomDataShareChecksum = minerSettings.RandomDataShareChecksum;
            var walletAddressDataSize = minerSettings.WalletAddressDataSize;

            RandomNumberGenerator.Fill(pocRandomData.Slice(16, randomDataShareChecksum));

            walletAddress[..walletAddressDataSize].CopyTo(pocRandomData.Slice(16 + randomDataShareChecksum, walletAddressDataSize));

            // block height
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(pocRandomData), 16 + randomDataShareChecksum + walletAddressDataSize), blockTemplate.CurrentBlockHeight);

            // nonce
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(pocRandomData), 16 + randomDataShareChecksum + walletAddressDataSize + 8), nonce);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdatePocRandomData(BlockTemplate blockTemplate, Span<byte> pocRandomData, long timestamp, long nonce)
        {
            if (!_isUpdatePocRandomDataAvailable)
            {
                SoftwareUpdatePocRandomData(blockTemplate, pocRandomData, timestamp, nonce);
                return;
            }

            try
            {
                var minerSettings = blockTemplate.MiningSettings;
                Native.XirophtDecentralizedCpuMiner_UpdatePocRandomData(MemoryMarshal.GetReference(pocRandomData), timestamp, minerSettings.RandomDataShareChecksum, minerSettings.WalletAddressDataSize, nonce);
            }
            catch (Exception)
            {
                _isUpdatePocRandomDataAvailable = false;
                SoftwareUpdatePocRandomData(blockTemplate, pocRandomData, timestamp, nonce);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareUpdatePocRandomData(BlockTemplate blockTemplate, Span<byte> pocRandomData, long timestamp, long nonce)
        {
            var minerSettings = blockTemplate.MiningSettings;

            // timestamp
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(pocRandomData), 8), timestamp);

            // nonce
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(pocRandomData), 16 + minerSettings.RandomDataShareChecksum + minerSettings.WalletAddressDataSize + 8), nonce);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DoPowShare(BlockTemplate blockTemplate, in XirophtDataBuffer dataBuffer, ReadOnlySpan<byte> blockchainMarkKey, MiningPowShare miningPowShare, string walletAddress, long nonce, long timestamp)
        {
            var pocShareIv = dataBuffer.PocShareIv;
            var pocShareIvSize = 8;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(pocShareIv), nonce);

            var miningSettings = blockTemplate.MiningSettings;
            var miningInstructions = miningSettings.MiningInstructions;

            var pocRandomData = dataBuffer.PocRandomData;
            var pocRandomDataSize = miningSettings.RandomDataShareSize;

            foreach (var miningInstruction in miningInstructions)
            {
                switch (miningInstruction)
                {
                    case MiningInstruction.DoNonceIv:
                        DoNonceIvMiningInstruction(pocShareIv, ref pocShareIvSize, miningSettings.PocRoundShaNonce);
                        break;

                    case MiningInstruction.DoNonceIvXor:
                        DoNonceIvXorMiningInstruction(pocShareIv, pocShareIvSize);
                        break;

                    case MiningInstruction.DoNonceIvEasySquareMath:
                        if (!DoNonceIvEasySquareMathMiningInstruction(blockTemplate, pocShareIv, ref pocShareIvSize, dataBuffer.PocShareWorkToDoBytes, dataBuffer.CurrentBlockDifficulty, dataBuffer.PreviousBlockFinalTransactionHash)) return false;
                        break;

                    case MiningInstruction.DoLz4CompressNonceIv:
                        DoLz4CompressNonceIvMiningInstruction(pocShareIv, ref pocShareIvSize);
                        break;

                    case MiningInstruction.DoNonceIvIterations:
                        DoNonceIvIterationsMiningInstruction(pocShareIv, ref pocShareIvSize, blockchainMarkKey, miningSettings.PocShareNonceIvIteration);
                        break;

                    case MiningInstruction.DoEncryptedPocShare:
                        DoEncryptedPocShare(dataBuffer.PreviousBlockFinalTransactionHash, pocShareIv[..pocShareIvSize], miningSettings.PowRoundAesShare, pocRandomData, ref pocRandomDataSize);
                        break;
                }
            }

            pocRandomData = pocRandomData[miningSettings.RandomDataShareSize..];

            var pocShare = Convert.ToHexString(pocRandomData[..pocRandomDataSize]);
            Sha3Utility.TryComputeSha512Hash(pocRandomData[..pocRandomDataSize], pocRandomData, out pocRandomDataSize);
            var shareDifficulty = BigInteger.Divide(BigInteger.Divide(ShaPowCalculation, blockTemplate.CurrentBlockDifficulty), BigInteger.Divide(new BigInteger(pocRandomData[..pocRandomDataSize]), blockTemplate.CurrentBlockDifficulty));

            miningPowShare.WalletAddress = walletAddress;
            miningPowShare.BlockHeight = blockTemplate.CurrentBlockHeight;
            miningPowShare.BlockHash = blockTemplate.CurrentBlockHash;
            miningPowShare.Nonce = nonce;
            miningPowShare.PoWaCShare = pocShare;
            miningPowShare.PoWaCShareDifficulty = shareDifficulty;
            miningPowShare.NonceComputedHexString = Convert.ToHexString(pocShareIv[..pocShareIvSize]);
            miningPowShare.Timestamp = timestamp;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoNonceIvMiningInstruction(Span<byte> pocShareIv, ref int pocShareIvSize, int pocRoundShaNonce)
        {
            if (!_isNonceIvAvailable)
            {
                SoftwareDoNonceIvMiningInstruction(pocShareIv, ref pocShareIvSize, pocRoundShaNonce);
                return;
            }

            try
            {
                Native.XirophtDecentralizedCpuMiner_DoNonceIvMiningInstruction(MemoryMarshal.GetReference(pocShareIv), ref pocShareIvSize, pocRoundShaNonce);
            }
            catch (Exception)
            {
                _isNonceIvAvailable = false;
                SoftwareDoNonceIvMiningInstruction(pocShareIv, ref pocShareIvSize, pocRoundShaNonce);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareDoNonceIvMiningInstruction(Span<byte> pocShareIv, ref int pocShareIvSize, int pocRoundShaNonce)
        {
            for (var i = pocRoundShaNonce - 1; i >= 0; i--)
            {
                Sha3Utility.TryComputeSha512Hash(pocShareIv[..pocShareIvSize], pocShareIv, out pocShareIvSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoNonceIvXorMiningInstruction(Span<byte> pocShareIv, int pocShareIvSize)
        {
            if (!_isNonceIvXorAvailable)
            {
                SoftwareDoNonceIvXorMiningInstruction(pocShareIv, pocShareIvSize);
                return;
            }

            try
            {
                Native.XirophtDecentralizedCpuMiner_DoNonceIvXorMiningInstruction(MemoryMarshal.GetReference(pocShareIv), pocShareIvSize);
            }
            catch (Exception)
            {
                _isNonceIvXorAvailable = false;
                SoftwareDoNonceIvXorMiningInstruction(pocShareIv, pocShareIvSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareDoNonceIvXorMiningInstruction(Span<byte> pocShareIv, int pocShareIvSize)
        {
            var (length, remainder) = Math.DivRem(pocShareIvSize, 2);

            for (var i = length - 1; i >= 0; i--)
            {
                var value = pocShareIv[i] ^ pocShareIv[pocShareIvSize - 1 - i];

                pocShareIv[i] = (byte) value;
                pocShareIv[pocShareIvSize - 1 - i] = (byte) value;
            }

            if (remainder != 0)
            {
                pocShareIv[length] = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DoNonceIvEasySquareMathMiningInstruction(BlockTemplate blockTemplate, Span<byte> pocShareIv, ref int pocShareIvSize, Span<byte> pocShareWorkToDoBytes, ReadOnlySpan<byte> currentBlockDifficulty, ReadOnlySpan<byte> previousFinalBlockTransactionHashKey)
        {
            if (!_isNonceIvEasySquareMathAvailable) return SoftwareDoNonceIvEasySquareMathMiningInstruction(blockTemplate, pocShareIv, ref pocShareIvSize, pocShareWorkToDoBytes, currentBlockDifficulty, previousFinalBlockTransactionHashKey);

            try
            {
                var minerSettings = blockTemplate.MiningSettings;

                var result = Native.XirophtDecentralizedCpuMiner_DoNonceIvEasySquareMathMiningInstruction(
                    minerSettings.PocShareNonceMaxSquareRetry,
                    minerSettings.PocShareNonceNoSquareFoundShaRounds,
                    minerSettings.PocShareNonceMin,
                    minerSettings.PocShareNonceMax,
                    blockTemplate.CurrentBlockHeight,
                    MemoryMarshal.GetReference(pocShareIv),
                    ref pocShareIvSize,
                    MemoryMarshal.GetReference(pocShareWorkToDoBytes),
                    MemoryMarshal.GetReference(currentBlockDifficulty),
                    currentBlockDifficulty.Length,
                    MemoryMarshal.GetReference(previousFinalBlockTransactionHashKey),
                    previousFinalBlockTransactionHashKey.Length
                );

                return result != 0;
            }
            catch (Exception)
            {
                _isNonceIvEasySquareMathAvailable = false;
                return SoftwareDoNonceIvEasySquareMathMiningInstruction(blockTemplate, pocShareIv, ref pocShareIvSize, pocShareWorkToDoBytes, currentBlockDifficulty, previousFinalBlockTransactionHashKey);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SoftwareDoNonceIvEasySquareMathMiningInstruction(BlockTemplate blockTemplate, Span<byte> pocShareIv, ref int pocShareIvSize, Span<byte> pocShareWorkToDoBytes, ReadOnlySpan<byte> currentBlockDifficulty, ReadOnlySpan<byte> previousFinalBlockTransactionHashKey)
        {
            var totalRetry = 0;

            var minerSettings = blockTemplate.MiningSettings;

            var newNonceGenerated = false;
            var newNonce = 0L;

            while (totalRetry < minerSettings.PocShareNonceMaxSquareRetry)
            {
                pocShareIv[..pocShareIvSize].CopyTo(pocShareWorkToDoBytes);
                currentBlockDifficulty.CopyTo(pocShareWorkToDoBytes[pocShareIvSize..]);

                // Block Height
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(pocShareWorkToDoBytes), pocShareIvSize + currentBlockDifficulty.Length), blockTemplate.CurrentBlockHeight);

                previousFinalBlockTransactionHashKey.CopyTo(pocShareWorkToDoBytes[(pocShareIvSize + currentBlockDifficulty.Length + 8)..]);

                Sha3Utility.TryComputeSha512Hash(pocShareWorkToDoBytes[..(pocShareIvSize + currentBlockDifficulty.Length + 8 + previousFinalBlockTransactionHashKey.Length)], pocShareWorkToDoBytes, out var _);

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

                Sha3Utility.TryComputeSha512Hash(pocShareIv[..pocShareIvSize], pocShareIv, out pocShareIvSize);
                totalRetry++;
            }

            if (!newNonceGenerated)
            {
                for (var i = minerSettings.PocShareNonceNoSquareFoundShaRounds - 1; i >= 0; i--)
                {
                    Sha3Utility.TryComputeSha512Hash(pocShareIv[..pocShareIvSize], pocShareIv, out pocShareIvSize);
                }

                
                newNonce = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(pocShareIv));
            }

            if (newNonce < minerSettings.PocShareNonceMin || newNonce > minerSettings.PocShareNonceMax) return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(pocShareIv), newNonce);
            pocShareIvSize = 8;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoLz4CompressNonceIvMiningInstruction(Span<byte> pocShareIv, ref int pocShareIvSize)
        {
            if (!_isLz4CompressNonceIvAvailable)
            {
                SoftwareDoLz4CompressNonceIvMiningInstruction(pocShareIv, ref pocShareIvSize);
                return;
            }

            try
            {
                Native.XirophtDecentralizedCpuMiner_DoLz4CompressNonceIvMiningInstruction(MemoryMarshal.GetReference(pocShareIv), ref pocShareIvSize);
            }
            catch (Exception)
            {
                _isLz4CompressNonceIvAvailable = false;
                SoftwareDoLz4CompressNonceIvMiningInstruction(pocShareIv, ref pocShareIvSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareDoLz4CompressNonceIvMiningInstruction(Span<byte> pocShareIv, ref int pocShareIvSize)
        {
            var newPocShareIv = ArrayPool<byte>.Shared.Rent(pocShareIvSize);

            try
            {
                pocShareIv[..pocShareIvSize].CopyTo(newPocShareIv);

                var output = LZ4Codec.Wrap(newPocShareIv, 0, pocShareIvSize);
                output.CopyTo(pocShareIv);

                pocShareIvSize = output.Length;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(newPocShareIv);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoNonceIvIterationsMiningInstruction(Span<byte> pocShareIv, ref int pocShareIvSize, ReadOnlySpan<byte> blockchainMarkKey, int pocShareNonceIvIteration)
        {
            if (!_isNonceIvIterationsAvailable)
            {
                SoftwareDoNonceIvIterationsMiningInstruction(pocShareIv, ref pocShareIvSize, blockchainMarkKey, pocShareNonceIvIteration);
                return;
            }

            try
            {
                Native.XirophtDecentralizedCpuMiner_DoNonceIvIterationsMiningInstruction(MemoryMarshal.GetReference(pocShareIv), ref pocShareIvSize, MemoryMarshal.GetReference(blockchainMarkKey), blockchainMarkKey.Length, pocShareNonceIvIteration, 16);
            }
            catch (Exception)
            {
                _isNonceIvIterationsAvailable = false;
                SoftwareDoNonceIvIterationsMiningInstruction(pocShareIv, ref pocShareIvSize, blockchainMarkKey, pocShareNonceIvIteration);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareDoNonceIvIterationsMiningInstruction(Span<byte> pocShareIv, ref int pocShareIvSize, ReadOnlySpan<byte> blockchainMarkKey, int pocShareNonceIvIteration)
        {
            Rfc2898DeriveBytes.Pbkdf2(pocShareIv[..pocShareIvSize], blockchainMarkKey, pocShareIv[..16], pocShareNonceIvIteration, HashAlgorithmName.SHA1);
            pocShareIvSize = 16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoEncryptedPocShare(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, int iterations, Span<byte> data, ref int dataSize)
        {
            if (!_isEncryptedPocShareAvailable)
            {
                SoftwareDoEncryptedPocShare(key, iv, iterations, data, ref dataSize);
                return;
            }

            try
            {
                Native.XirophtDecentralizedCpuMiner_DoEncryptedPocShareMiningInstruction(MemoryMarshal.GetReference(key), MemoryMarshal.GetReference(iv), iterations, MemoryMarshal.GetReference(data), ref dataSize);
            }
            catch (Exception)
            {
                _isEncryptedPocShareAvailable = false;
                SoftwareDoEncryptedPocShare(key, iv, iterations, data, ref dataSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SoftwareDoEncryptedPocShare(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, int iterations, Span<byte> data, ref int dataSize)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = key.ToArray();
            aes.BlockSize = 128;

            data[..dataSize].CopyTo(data.Slice(dataSize, dataSize));
            var newData = data[dataSize..];

            for (var i = iterations - 1; i >= 0; i--)
            {
                var packetLength = dataSize;
                var paddingSizeRequired = 16 - packetLength % 16;
                var paddedLength = packetLength + paddingSizeRequired;

                newData.Slice(packetLength, paddingSizeRequired).Fill((byte) paddingSizeRequired);
                dataSize = aes.EncryptCfb(newData[..paddedLength], iv, newData, PaddingMode.None, 128);
            }
        }

        protected override void ExecuteJob(IJobTemplate jobTemplate, CancellationToken cancellationToken)
        {
            if (jobTemplate is not BlockTemplate blockTemplate) return;

            var miningSettings = blockTemplate.MiningSettings;

            // Configure stack based buffer size:
            var pocRandomDataBufferSize = miningSettings.RandomDataShareSize;

            for (var i = miningSettings.PowRoundAesShare - 1; i >= 0; i--)
            {
                pocRandomDataBufferSize += 16 - pocRandomDataBufferSize % 16;
            }

            pocRandomDataBufferSize += miningSettings.RandomDataShareSize;

            const int nonceBufferSize = 64 + 64 + 16 + 8;

            var currentBlockDifficultyBufferSize = blockTemplate.CurrentBlockDifficulty.GetByteCount();
            var previousBlockFinalTransactionHashBufferSize = Encoding.ASCII.GetMaxByteCount(blockTemplate.PreviousBlockFinalTransactionHash.Length);
            var pocShareWorkToDoBytesBufferSize = 64 + currentBlockDifficultyBufferSize + 8 + 32;

            var totalBufferSize = pocRandomDataBufferSize + nonceBufferSize + pocShareWorkToDoBytesBufferSize + currentBlockDifficultyBufferSize + previousBlockFinalTransactionHashBufferSize;

            if (totalBufferSize > MaxStackSize)
            {
                if (_heapDataBuffer.Length < totalBufferSize) _heapDataBuffer = new byte[totalBufferSize];
            }

            var xirophtDataBuffer = new XirophtDataBuffer(totalBufferSize > MaxStackSize ? _heapDataBuffer : stackalloc byte[totalBufferSize], pocRandomDataBufferSize, nonceBufferSize, pocShareWorkToDoBytesBufferSize, blockTemplate.CurrentBlockDifficulty, blockTemplate.PreviousBlockFinalTransactionHash);

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
                    GeneratePocRandomData(blockTemplate, xirophtDataBuffer.PocRandomData, _walletAddressBytes, currentNonce, timestamp);
                }
                else
                {
                    UpdatePocRandomData(blockTemplate, xirophtDataBuffer.PocRandomData, timestamp, currentNonce);
                }

                if (DoPowShare(blockTemplate, xirophtDataBuffer, _blockchainMarkKey, miningPowShare, _walletAddress, currentNonce, timestamp))
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