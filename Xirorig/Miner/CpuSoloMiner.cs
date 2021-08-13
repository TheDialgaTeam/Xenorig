using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using TheDialgaTeam.Serilog.Formatting.Ansi;
using Xirorig.Algorithm;
using Xirorig.Network.Api.Models;
using Xirorig.Utility;

namespace Xirorig.Miner
{
    internal class CpuSoloMiner : IDisposable
    {
        public event Action<string, bool, object[]>? Log;
        public event Action<long, string>? BlockFound;

        private readonly int _threadId;
        private readonly ThreadPriority _threadPriority;
        private Thread? _currentThread;

        private bool _isActive;
        private CancellationTokenSource? _cancellationTokenSource;

        // CpuSoloMiner shared thread variable.
        private BlockTemplate? _blockTemplate;
        private string? _walletAddress;
        private IAlgorithm? _algorithm;

        private bool _isNewBlockAvailable;

        private long _totalHashCalculatedIn10Seconds;
        private long _totalHashCalculatedIn60Seconds;
        private long _totalHashCalculatedIn15Minutes;

        public long TotalHashCalculatedIn10Seconds
        {
            get => Environment.Is64BitProcess ? _totalHashCalculatedIn10Seconds : Interlocked.Read(ref _totalHashCalculatedIn10Seconds);
            set
            {
                if (Environment.Is64BitProcess)
                {
                    _totalHashCalculatedIn10Seconds = value;
                }
                else
                {
                    Interlocked.Exchange(ref _totalHashCalculatedIn10Seconds, value);
                }
            }
        }

        public long TotalHashCalculatedIn60Seconds
        {
            get => Environment.Is64BitProcess ? _totalHashCalculatedIn60Seconds : Interlocked.Read(ref _totalHashCalculatedIn60Seconds);
            set
            {
                if (Environment.Is64BitProcess)
                {
                    _totalHashCalculatedIn60Seconds = value;
                }
                else
                {
                    Interlocked.Exchange(ref _totalHashCalculatedIn60Seconds, value);
                }
            }
        }

        public long TotalHashCalculatedIn15Minutes
        {
            get => Environment.Is64BitProcess ? _totalHashCalculatedIn15Minutes : Interlocked.Read(ref _totalHashCalculatedIn15Minutes);
            set
            {
                if (Environment.Is64BitProcess)
                {
                    _totalHashCalculatedIn15Minutes = value;
                }
                else
                {
                    Interlocked.Exchange(ref _totalHashCalculatedIn15Minutes, value);
                }
            }
        }

        public CpuSoloMiner(int threadId, ThreadPriority threadPriority)
        {
            _threadId = threadId;
            _threadPriority = threadPriority;
        }

        public void StartMining(string walletAddress, IAlgorithm algorithm)
        {
            if (_isActive) return;
            _isActive = true;

            _cancellationTokenSource = new CancellationTokenSource();
            _walletAddress = walletAddress;
            _algorithm = algorithm;

            _currentThread = new Thread(RunMiner) { Name = $"Cpu Solo Miner Thread {_threadId}", IsBackground = true, Priority = _threadPriority };
            _currentThread.Start();
        }

        public void StopMining()
        {
            if (!_isActive) return;
            _isActive = false;

            _cancellationTokenSource?.Cancel();
            _currentThread?.Join();

            Dispose();
        }

        public void UpdateBlockTemplate(BlockTemplate blockTemplate)
        {
            Interlocked.Exchange(ref _blockTemplate, blockTemplate);
            _isNewBlockAvailable = true;
        }

        private void RunMiner()
        {
            if (_cancellationTokenSource == null) throw new NullReferenceException();
            if (_walletAddress == null) throw new NullReferenceException();
            if (_algorithm == null) throw new NullReferenceException();

            // Static Miner working variable.
            var cancellationToken = _cancellationTokenSource.Token;

            var rijndael = Rijndael.Create();

            var randomNumberGenerator = RandomNumberGenerator.Create();
            var randomNumberGeneratorBytes = new byte[1];

            var pocRandomData = Array.Empty<byte>();

            var walletAddress = _walletAddress;
            var walletAddressBytes = Base58Utility.DecodeWithoutChecksum(walletAddress);

            var algorithm = _algorithm;

            var miningPowShare = new MiningPowShare();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for new block.
                while (!_isNewBlockAvailable)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    Thread.Sleep(1);
                }

                if (cancellationToken.IsCancellationRequested) break;

                // Received new block!
                _isNewBlockAvailable = false;

                // Miner working variable.
                var currentBlockTemplate = _blockTemplate;
                if (currentBlockTemplate == null) continue;

                var currentMinerSettings = currentBlockTemplate.MiningSettings;

                if (pocRandomData.Length != currentMinerSettings.RandomDataShareSize)
                {
                    pocRandomData = new byte[currentMinerSettings.RandomDataShareSize];
                }

                var previousBlockFinalTransactionHashBytes = Encoding.ASCII.GetBytes(currentBlockTemplate.PreviousBlockFinalTransactionHash);
                previousBlockFinalTransactionHashBytes = Sha3Utility.ComputeSha3512Hash(previousBlockFinalTransactionHashBytes);
                Array.Resize(ref previousBlockFinalTransactionHashBytes, 32);

                var minNonce = currentMinerSettings.PocShareNonceMin;
                var maxNonce = currentMinerSettings.PocShareNonceMax;

                var currentNonce = currentMinerSettings.PocShareNonceMin;

                LogInformation($"{AnsiEscapeCodeConstants.BlueForegroundColor}Thread: {{ThreadId}} | Job Difficulty: {{JobDifficulty:l}}{AnsiEscapeCodeConstants.Reset}", _threadId, currentBlockTemplate.CurrentBlockDifficulty.ToString());

                while (!_isNewBlockAvailable)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

                    if (currentNonce == minNonce)
                    {
                        MiningUtility.GeneratePocRandomData(pocRandomData, randomNumberGeneratorBytes, randomNumberGenerator, currentBlockTemplate, walletAddressBytes, currentNonce, timestamp);
                    }
                    else
                    {
                        MiningUtility.UpdatePocRandomData(pocRandomData, currentBlockTemplate, currentNonce, timestamp);
                    }

                    if (MiningUtility.DoPowShare(miningPowShare, currentBlockTemplate, rijndael, algorithm, walletAddress, currentNonce, timestamp, pocRandomData, previousBlockFinalTransactionHashBytes))
                    {
                        Interlocked.Increment(ref _totalHashCalculatedIn10Seconds);
                        Interlocked.Increment(ref _totalHashCalculatedIn60Seconds);
                        Interlocked.Increment(ref _totalHashCalculatedIn15Minutes);

                        if (miningPowShare.PoWaCShareDifficulty >= currentBlockTemplate.CurrentBlockDifficulty)
                        {
                            var shareJson = JsonConvert.SerializeObject(new MiningShare
                            {
                                MiningPowShareObject = miningPowShare,
                                PacketTimestamp = miningPowShare.Timestamp
                            });

                            BlockFound?.Invoke(currentBlockTemplate.CurrentBlockHeight, shareJson);
                            LogInformation($"{AnsiEscapeCodeConstants.GreenForegroundColor}Thread: {{ThreadId}} | Block Found | Nonce: {{Nonce}} | Diff: {{ShareDifficulty:l}}{AnsiEscapeCodeConstants.Reset}", _threadId, currentNonce, miningPowShare.PoWaCShareDifficulty.ToString());
                            break;
                        }
                    }

                    currentNonce++;

                    if (currentNonce > maxNonce)
                    {
                        currentNonce = minNonce;
                    }
                }
            }

            randomNumberGenerator.Dispose();
            rijndael.Dispose();
        }

        private void LogInformation(string message, params object[] args)
        {
            Log?.Invoke(message, true, args);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}