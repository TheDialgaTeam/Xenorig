using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Timers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Config;
using TheDialgaTeam.Xiropht.Xirorig.Network;
using Xiropht_Connector_All.SoloMining;
using Timer = System.Timers.Timer;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner
{
    public class CpuSoloMiners
    {
        private readonly XirorigConfiguration _xirorigConfiguration;
        private readonly XirorigToSeedNetwork _xirorigToSeedNetwork;
        private readonly ILogger<CpuSoloMiners> _logger;

        private long _blockId;
        private string? _blockTimestampCreate;
        private string? _blockKey;
        private string? _blockIndication;
        private decimal _blockDifficulty;

        private decimal _jobMinRange;
        private decimal _jobMaxRange;
        private int _jobAesRound;
        private int _jobAesSize;
        private string? _jobAesKey;
        private string? _jobXorKey;

        private bool _isActive;
        private readonly string _userAgent;

        private Timer? _calculateAverage10SecondsHashTimer;
        private Timer? _calculateAverage60SecondsHashTimer;
        private Timer? _calculateAverage15MinutesHashTimer;
        private Timer? _printAverageHashTimer;

        private readonly ConcurrentDictionary<int, int> _totalAverage10SecondsHashesCalculated = new ConcurrentDictionary<int, int>();
        private readonly ConcurrentDictionary<int, int> _totalAverage60SecondsHashesCalculated = new ConcurrentDictionary<int, int>();
        private readonly ConcurrentDictionary<int, int> _totalAverage15MinutesHashesCalculated = new ConcurrentDictionary<int, int>();

        private float _maxHash;

        private readonly Thread[] _miningThreads;
        private readonly bool[] _miningThreadWaitForNextJob;

        public Dictionary<int, float> Average10SecondsHashesCalculated { get; } = new Dictionary<int, float>();

        public Dictionary<int, float> Average60SecondsHashesCalculated { get; } = new Dictionary<int, float>();

        public Dictionary<int, float> Average15MinutesHashesCalculated { get; } = new Dictionary<int, float>();

        public CpuSoloMiners(XirorigConfiguration xirorigConfiguration, XirorigToSeedNetwork xirorigToSeedNetwork, ILogger<CpuSoloMiners> logger)
        {
            _xirorigConfiguration = xirorigConfiguration;
            _xirorigToSeedNetwork = xirorigToSeedNetwork;
            _logger = logger;

            _miningThreads = new Thread[_xirorigConfiguration.Threads.Length];
            _miningThreadWaitForNextJob = new bool[_xirorigConfiguration.Threads.Length];
            _userAgent = $"Xirorig/{Assembly.GetExecutingAssembly().GetName().Version}";
        }

        public void StartMinerService()
        {
            if (_isActive) return;
            _isActive = true;

            _calculateAverage10SecondsHashTimer = new Timer { AutoReset = true, Enabled = true, Interval = TimeSpan.FromSeconds(10).TotalMilliseconds };
            _calculateAverage60SecondsHashTimer = new Timer { AutoReset = true, Enabled = true, Interval = TimeSpan.FromSeconds(60).TotalMilliseconds };
            _calculateAverage15MinutesHashTimer = new Timer { AutoReset = true, Enabled = true, Interval = TimeSpan.FromMinutes(15).TotalMilliseconds };
            _printAverageHashTimer = new Timer { AutoReset = true, Enabled = true, Interval = TimeSpan.FromSeconds(_xirorigConfiguration.PrintTime).TotalMilliseconds };

            _calculateAverage10SecondsHashTimer.Elapsed += CalculateAverage10SecondsHashTimerOnElapsed;
            _calculateAverage60SecondsHashTimer.Elapsed += CalculateAverage60SecondsHashTimerOnElapsed;
            _calculateAverage15MinutesHashTimer.Elapsed += CalculateAverage15MinutesHashTimerOnElapsed;
            _printAverageHashTimer.Elapsed += PrintAverageHashTimerOnElapsed;

            AllocateMiningThreads();
        }

        public void UpdateJob(JObject jobObject)
        {
            _blockId = jobObject["ID"]!.Value<long>();
            _blockTimestampCreate = jobObject["TIMESTAMP"]!.Value<string>();
            _blockKey = jobObject["KEY"]!.Value<string>();
            _blockIndication = jobObject["INDICATION"]!.Value<string>();
            _blockDifficulty = jobObject["DIFFICULTY"]!.Value<decimal>();

            var job = jobObject["JOB"]!.Value<string>().Split(';', StringSplitOptions.RemoveEmptyEntries);

            _jobMinRange = decimal.Parse(job[0]);
            _jobMaxRange = decimal.Parse(job[1]);

            _jobAesRound = jobObject["AESROUND"]!.Value<int>();
            _jobAesSize = jobObject["AESSIZE"]!.Value<int>();
            _jobAesKey = jobObject["AESKEY"]!.Value<string>();
            _jobXorKey = jobObject["XORKEY"]!.Value<string>();

            for (var i = 0; i < _xirorigConfiguration.Threads.Length; i++)
            {
                _miningThreadWaitForNextJob[i] = false;
            }
        }

        private void CalculateAverage10SecondsHashTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var state = _totalAverage10SecondsHashesCalculated.Values.ToImmutableArray();

            for (var i = 0; i < state.Length; i++)
            {
                Average10SecondsHashesCalculated[i] = state[i] / 10f;
                _totalAverage10SecondsHashesCalculated[i] = 0;
            }
        }

        private void CalculateAverage60SecondsHashTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var state = _totalAverage60SecondsHashesCalculated.Values.ToImmutableArray();

            for (var i = 0; i < state.Length; i++)
            {
                Average60SecondsHashesCalculated[i] = state[i] / 60f;
                _totalAverage60SecondsHashesCalculated[i] = 0;
            }
        }

        private void CalculateAverage15MinutesHashTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var state = _totalAverage15MinutesHashesCalculated.Values.ToImmutableArray();

            for (var i = 0; i < state.Length; i++)
            {
                Average15MinutesHashesCalculated[i] = state[i] / 900f;
                _totalAverage15MinutesHashesCalculated[i] = 0;
            }
        }

        private void PrintAverageHashTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            float average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;
            var threadCount = Average10SecondsHashesCalculated.Count;

            for (var i = 0; i < threadCount; i++)
            {
                average10SecondsSum += Average10SecondsHashesCalculated[i];
                average60SecondsSum += Average60SecondsHashesCalculated[i];
                average15MinutesSum += Average15MinutesHashesCalculated[i];
            }

            _maxHash = MathF.Max(_maxHash, average10SecondsSum);

            _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m speed 10s/60s/15m \u001b[36;1m{average10SecondsSum:F0}\u001b[0m \u001b[36m{average60SecondsSum:F0} {average15MinutesSum:F0}\u001b[0m \u001b[36;1mH/s\u001b[0m max \u001b[36;1m{maxHash:F0}\u001b[0m", DateTimeOffset.Now, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHash);
        }

        private void AllocateMiningThreads()
        {
            var jobThreads = _xirorigConfiguration.Threads;

            for (var i = 0; i < jobThreads.Length; i++)
            {
                Average10SecondsHashesCalculated[i] = 0;
                Average60SecondsHashesCalculated[i] = 0;
                Average15MinutesHashesCalculated[i] = 0;

                _totalAverage10SecondsHashesCalculated[i] = 0;
                _totalAverage60SecondsHashesCalculated[i] = 0;
                _totalAverage15MinutesHashesCalculated[i] = 0;

                _miningThreads[i] = new Thread(MiningThreadTask) { IsBackground = true, Priority = jobThreads[i].ThreadPriority };
                _miningThreads[i].Start((this, i));

                _miningThreadWaitForNextJob[i] = true;
            }

            _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[32;1mREADY (CPU)\u001b[0m threads \u001b[36;1m{numThreads}\u001b[0m", DateTimeOffset.Now, _xirorigConfiguration.Threads.Length);
        }

        private void MiningThreadTask(object? state)
        {
            if (!(state is (CpuSoloMiners cpuSoloMiners, int threadId))) return;

            var miningThread = cpuSoloMiners._xirorigConfiguration.Threads[threadId];
            var logger = cpuSoloMiners._logger;
            var xirorigToSeedNetwork = cpuSoloMiners._xirorigToSeedNetwork;
            var userAgent = cpuSoloMiners._userAgent;

            var totalAverage10SecondsHashesCalculated = cpuSoloMiners._totalAverage10SecondsHashesCalculated;
            var totalAverage60SecondsHashesCalculated = cpuSoloMiners._totalAverage60SecondsHashesCalculated;
            var totalAverage15MinutesHashesCalculated = cpuSoloMiners._totalAverage15MinutesHashesCalculated;

            var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var sha512 = SHA512.Create();
            var easyBlockRandomNumber = new decimal[256];
            var randomNumberBuffer = new byte[1];

            const string additionOperator = " + ";
            const string subtractionOperator = " - ";
            const string multiplicationOperator = " * ";
            const string divisionOperator = " / ";
            const string moduloOperator = " % ";

            try
            {
                while (_isActive)
                {
                    while (cpuSoloMiners._miningThreadWaitForNextJob[threadId])
                    {
                        Thread.Sleep(1000);
                    }

                    cpuSoloMiners._miningThreadWaitForNextJob[threadId] = true;

                    // Miner receive a job! Now initialize the job parameters.
                    var currentBlockId = cpuSoloMiners._blockId;
                    var currentBlockTimestampCreate = cpuSoloMiners._blockTimestampCreate;
                    var currentBlockKey = cpuSoloMiners._blockKey;
                    var currentBlockIndication = cpuSoloMiners._blockIndication;
                    var currentBlockDifficulty = cpuSoloMiners._blockDifficulty;

                    var currentJobMinRange = cpuSoloMiners._jobMinRange;
                    var currentJobMaxRange = cpuSoloMiners._jobMaxRange;
                    var currentJobAesRound = cpuSoloMiners._jobAesRound;
                    var currentJobAesSize = cpuSoloMiners._jobAesSize;
                    var currentJobAesKey = cpuSoloMiners._jobAesKey;
                    var currentJobXorKey = cpuSoloMiners._jobXorKey;

                    ICryptoTransform currentAesCryptoTransform;
                    var blockFound = false;

                    using (var pdb = new PasswordDeriveBytes(currentBlockKey!, Encoding.UTF8.GetBytes(currentJobAesKey!)))
                    {
#pragma warning disable 618
                        var aes = new RijndaelManaged { BlockSize = currentJobAesSize, KeySize = currentJobAesSize, Key = pdb.GetBytes(currentJobAesSize / 8), IV = pdb.GetBytes(currentJobAesSize / 8) };
                        currentAesCryptoTransform = aes.CreateEncryptor();
#pragma warning restore 618
                    }

                    var startRange = Math.Floor(cpuSoloMiners._jobMaxRange * miningThread.MinMiningRangePercentage * 0.01m) + cpuSoloMiners._jobMinRange;
                    var endRange = Math.Min(cpuSoloMiners._jobMaxRange, Math.Floor(cpuSoloMiners._jobMaxRange * miningThread.MaxMiningRangePercentage * 0.01m) + 1);

                    for (var i = 0; i < 256; i++)
                    {
                        easyBlockRandomNumber[i] = startRange + Math.Floor(Math.Max(0, i / 255m - 0.0000001m) * (endRange - startRange + 1));
                    }

                    void DoMathCalculation(decimal firstNumber, decimal secondNumber)
                    {
                        var additionResult = firstNumber + secondNumber;

                        if (additionResult >= currentJobMinRange && additionResult <= currentJobMaxRange)
                        {
                            ValidateAndSubmitShare(firstNumber, secondNumber, additionOperator, additionResult);
                        }

                        var multiplicationResult = firstNumber * secondNumber;

                        if (multiplicationResult >= currentJobMinRange && multiplicationResult <= currentJobMaxRange)
                        {
                            ValidateAndSubmitShare(firstNumber, secondNumber, multiplicationOperator, multiplicationResult);
                        }

                        if (firstNumber < secondNumber)
                        {
                            var moduloResult = firstNumber;

                            if (moduloResult >= currentJobMinRange && moduloResult <= currentJobMaxRange)
                            {
                                ValidateAndSubmitShare(firstNumber, secondNumber, moduloOperator, moduloResult);
                            }
                        }
                        else
                        {
                            var moduloResult = firstNumber % secondNumber;

                            if (moduloResult >= currentJobMinRange && moduloResult <= currentJobMaxRange)
                            {
                                ValidateAndSubmitShare(firstNumber, secondNumber, moduloOperator, moduloResult);
                            }
                        }

                        if (firstNumber <= secondNumber) return;

                        var subtractResult = firstNumber - secondNumber;

                        if (subtractResult >= currentJobMinRange && subtractResult <= currentJobMaxRange)
                        {
                            ValidateAndSubmitShare(firstNumber, secondNumber, subtractionOperator, subtractResult);
                        }

                        var divisionResult = firstNumber / secondNumber;

                        if (divisionResult < currentJobMinRange || divisionResult > currentJobMaxRange) return;

                        if (divisionResult == Math.Floor(divisionResult))
                        {
                            ValidateAndSubmitShare(firstNumber, secondNumber, divisionOperator, divisionResult);
                        }
                    }

                    void ValidateAndSubmitShare(decimal firstNumber, decimal secondNumber, string operatorSymbol, decimal result)
                    {
                        var encryptedShare = MiningUtility.MakeEncryptedShare($"{firstNumber}{operatorSymbol}{secondNumber}{currentBlockTimestampCreate}", currentJobXorKey!, currentJobAesRound, currentAesCryptoTransform, sha512);
                        var hashEncryptedShare = MiningUtility.ComputeHash(sha512, encryptedShare);

                        totalAverage10SecondsHashesCalculated[threadId]++;
                        totalAverage60SecondsHashesCalculated[threadId]++;
                        totalAverage15MinutesHashesCalculated[threadId]++;

                        if (!hashEncryptedShare.Equals(currentBlockIndication, StringComparison.OrdinalIgnoreCase)) return;
                        logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[32;1mThread: {threadId} | Job Type: {jobType:l} | Block found: {firstNumber}{operatorSymbol:l}{secondNumber} = {result}\u001b[0m", DateTimeOffset.Now, threadId + 1, "Random", firstNumber, operatorSymbol, secondNumber, result);

                        blockFound = true;

                        var packetBuilder = new StringBuilder();
                        packetBuilder.Append(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob);
                        packetBuilder.Append('|');
                        packetBuilder.Append(encryptedShare);
                        packetBuilder.Append('|');
                        packetBuilder.Append(result);
                        packetBuilder.Append('|');
                        packetBuilder.Append(firstNumber);
                        packetBuilder.Append(operatorSymbol);
                        packetBuilder.Append(secondNumber);
                        packetBuilder.Append('|');
                        packetBuilder.Append(hashEncryptedShare);
                        packetBuilder.Append('|');
                        packetBuilder.Append(currentBlockId);
                        packetBuilder.Append('|');
                        packetBuilder.Append(userAgent);

                        xirorigToSeedNetwork.SendPacketToNetworkAsync(packetBuilder.ToString(), true).GetAwaiter().GetResult();
                    }

                    try
                    {
                        if (threadId == 0)
                        {
                            logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[34;1mThread: {threadIndex} | Job Type: {jobType:l} | Job Difficulty: {JobDifficulty} | Job Range: {startRange}-{endRange}\u001b[0m", DateTimeOffset.Now, threadId + 1, "Easy Block", currentBlockDifficulty, startRange, endRange);

                            for (var i = 256 - 1; i >= 0; i--)
                            {
                                for (var j = 256 - 1; j >= 0; j--)
                                {
                                    DoMathCalculation(easyBlockRandomNumber[i], easyBlockRandomNumber[j]);
                                }
                            }
                        }

                        logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[34;1mThread: {threadIndex} | Job Type: {jobType:l} | Job Difficulty: {JobDifficulty} | Job Range: {startRange}-{endRange}\u001b[0m", DateTimeOffset.Now, threadId + 1, "Random", currentBlockDifficulty, startRange, endRange);

                        while (currentBlockIndication!.Equals(cpuSoloMiners._blockIndication, StringComparison.OrdinalIgnoreCase) && !blockFound)
                        {
                            var firstNumber = MiningUtility.GenerateNumberMathCalculation(rngCryptoServiceProvider, randomNumberBuffer, startRange, endRange);
                            var secondNumber = MiningUtility.GenerateNumberMathCalculation(rngCryptoServiceProvider, randomNumberBuffer, startRange, endRange);

                            DoMathCalculation(firstNumber, secondNumber);
                            DoMathCalculation(secondNumber, firstNumber);

                            for (var i = 256 - 1; i >= 0; i--)
                            {
                                if (!currentBlockIndication!.Equals(cpuSoloMiners._blockIndication, StringComparison.OrdinalIgnoreCase)) break;

                                DoMathCalculation(firstNumber, easyBlockRandomNumber[i]);
                                DoMathCalculation(secondNumber, easyBlockRandomNumber[i]);
                                DoMathCalculation(easyBlockRandomNumber[i], firstNumber);
                                DoMathCalculation(easyBlockRandomNumber[i], secondNumber);
                            }
                        }
                    }
                    finally
                    {
                        currentAesCryptoTransform.Dispose();
                    }
                }
            }
            finally
            {
                rngCryptoServiceProvider.Dispose();
                sha512.Dispose();
            }
        }
    }
}