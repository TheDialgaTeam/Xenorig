﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo;
using Xenolib.Algorithms.Xenophyte.Centralized.Utilities;
using Xenolib.Utilities;
using Xenorig.Options;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Solo.Miner;

public delegate void BlockSubmitResultHandler(long height, string jobType, bool isGoodBlock, string reason, double roundTripTime);

internal sealed partial class CpuMiner
{
    private static partial class Native
    {
        public static partial class Windows
        {
            [LibraryImport("kernel32")]
            public static partial nint GetCurrentThread();

            [LibraryImport("kernel32")]
            public static partial nint SetThreadAffinityMask(nint hThread, nint dwThreadAffinityMask);
        }

        public static partial class Linux
        {
            [LibraryImport("libc", EntryPoint = "sched_setaffinity")]
            public static partial int SetThreadAffinityMask(int pid, int cpuSetSize, in ulong mask);
        }
    }

    public event BlockSubmitResultHandler? FoundBlock;

    public const string JobTypeEasy = "Easy Block";
    public const string JobTypeSemiRandom = "Semi Random";
    public const string JobTypeRandom = "Random";

    public double[] AverageHashCalculatedIn10Seconds { get; }
    public double[] AverageHashCalculatedIn60Seconds { get; }
    public double[] AverageHashCalculatedIn15Minutes { get; }

    private const string InvalidShare = "Invalid Share";
    private const string OrphanShare = "Orphan Share";

    private readonly ILogger _logger;
    private readonly Options.Pool _pool;
    private readonly XenorigOptions _options;
    private readonly Network _network;

    private int _isCpuMinerActive;

    private readonly Thread[] _cpuMiningThreads;
    private readonly CpuMinerJob[] _cpuMinerJobs;

    private readonly Timer _calculateAverageHashTimer;

    private int _amountSampledFor60Seconds;
    private int _amountSampledFor15Minutes;

    private readonly long[] _totalHashCalculatedIn10Seconds;
    private readonly long[] _totalHashCalculatedIn60Seconds;
    private readonly long[] _totalHashCalculatedIn15Minutes;

    public CpuMiner(XenorigOptions options, ILogger logger, Options.Pool pool, Network network)
    {
        _logger = logger;
        _options = options;
        _pool = pool;
        _network = network;

        var totalThreads = options.Xenophyte_Centralized_Solo.CpuMiner.GetNumberOfThreads();

        _cpuMiningThreads = new Thread[totalThreads];
        _cpuMinerJobs = new CpuMinerJob[totalThreads];

        for (var i = 0; i < totalThreads; i++)
        {
            _cpuMinerJobs[i] = new CpuMinerJob();
        }

        _calculateAverageHashTimer = new Timer(CalculateAverageHashTimerOnElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        AverageHashCalculatedIn10Seconds = new double[totalThreads];
        AverageHashCalculatedIn60Seconds = new double[totalThreads];
        AverageHashCalculatedIn15Minutes = new double[totalThreads];

        _totalHashCalculatedIn10Seconds = new long[totalThreads];
        _totalHashCalculatedIn60Seconds = new long[totalThreads];
        _totalHashCalculatedIn15Minutes = new long[totalThreads];
    }

    public void StartCpuMiner()
    {
        if (Interlocked.CompareExchange(ref _isCpuMinerActive, 1, 0) == 1) return;

        var totalThreads = _options.Xenophyte_Centralized_Solo.CpuMiner.GetNumberOfThreads();

        for (var i = 0; i < totalThreads; i++)
        {
            var threadId = i;

            _cpuMiningThreads[i] = new Thread(() => ExecuteCpuMinerThread(threadId, _options.Xenophyte_Centralized_Solo.CpuMiner))
            {
                Name = $"Mining Thread {i}",
                IsBackground = true,
                Priority = _options.Xenophyte_Centralized_Solo.CpuMiner.GetThreadPriority(i)
            };

            _cpuMiningThreads[i].Start();
        }

        Logger.PrintCpuMinerReady(_logger, totalThreads);

        _calculateAverageHashTimer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    public void StopCpuMiner()
    {
        if (Interlocked.CompareExchange(ref _isCpuMinerActive, 0, 1) == 0) return;

        _calculateAverageHashTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void UpdateJobTemplate(BlockHeader blockHeader)
    {
        foreach (var cpuMinerJob in _cpuMinerJobs)
        {
            cpuMinerJob.Update(blockHeader);
        }
    }

    private void ExecuteCpuMinerThread(int threadId, Options.CpuMiner options)
    {
        var threadAffinity = options.GetThreadAffinity(threadId);

        if (threadAffinity != 0)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Native.Windows.SetThreadAffinityMask(Native.Windows.GetCurrentThread(), (nint) threadAffinity);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Native.Linux.SetThreadAffinityMask(0, sizeof(ulong), threadAffinity);
            }
        }

        // Thread Variable
        var cpuMinerJob = _cpuMinerJobs[threadId];

        var doEasyBlock = options.GetDoEasyBlock(threadId);
        var easyBlockIndex = options.GetEasyBlockIndex(threadId);
        var totalEasyBlockThreads = options.GetTotalEasyBlockThreads();
        var useXenophyteRandomizer = options.GetUseXenophyteRandomizer(threadId);

        while (_isCpuMinerActive == 1)
        {
            // Wait for new block.
            do
            {
                if (cpuMinerJob.HasNewBlock)
                {
                    cpuMinerJob.BlockFound = false;
                    cpuMinerJob.HasNewBlock = false;
                    break;
                }

                Thread.Sleep(1);
            } while (_isCpuMinerActive == 1);

            if (_isCpuMinerActive == 0) break;

            // Received new block!
            if (doEasyBlock)
            {
                DoEasyBlocksCalculations(threadId, easyBlockIndex, totalEasyBlockThreads, cpuMinerJob);

                if (cpuMinerJob.BlockFound) continue;
                if (cpuMinerJob.HasNewBlock) continue;
            }

            DoNonSmartMiningModeCalculations(threadId, useXenophyteRandomizer, cpuMinerJob);
        }
    }

    [SkipLocalsInit]
    private void DoEasyBlocksCalculations(int threadId, int easyBlockIndex, int totalEasyBlockThreads, CpuMinerJob cpuMinerJob)
    {
        cpuMinerJob.GenerateEasyBlockValues();

        var easyBlockValues = cpuMinerJob.EasyBlockValues;

        var (startIndex, size) = CpuMinerUtility.GetJobChunk(easyBlockValues.Length, totalEasyBlockThreads, easyBlockIndex);
        if (size == 0) return;

        Span<long> chunkData = stackalloc long[size];
        BufferUtility.MemoryCopy(easyBlockValues.Slice(startIndex, size), chunkData, size);

        Logger.PrintCurrentChunkedThreadJob(_logger, threadId, JobTypeEasy, startIndex, startIndex + size - 1, size);

        for (var i = size - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = cpuMinerJob.EasyBlockValues.Length - 1; j >= 0; j--)
            {
                var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                DoMathCalculations(threadId, chunkData.GetRef(choseRandom), easyBlockValues.GetRef(choseRandom2), JobTypeEasy, cpuMinerJob);

                (easyBlockValues.GetRef(j), easyBlockValues.GetRef(choseRandom2)) = (easyBlockValues.GetRef(choseRandom2), easyBlockValues.GetRef(j));
            }

            if (cpuMinerJob.BlockFound) return;
            if (cpuMinerJob.HasNewBlock) return;

            chunkData.GetRef(choseRandom) = chunkData.GetRef(i);
        }
    }

    private void DoNonSmartMiningModeCalculations(int threadId, bool useXenophyteRandomizer, CpuMinerJob cpuMinerJob)
    {
        var easyBlockValues = cpuMinerJob.EasyBlockValues;

        Logger.PrintCurrentThreadJob(_logger, threadId, JobTypeRandom);

        if (useXenophyteRandomizer)
        {
            do
            {
                long choseRandom;
                long choseRandom2;

                do
                {
                    choseRandom = RandomNumberGeneratorUtility.GetBiasRandomBetween(cpuMinerJob.BlockMinRange, cpuMinerJob.BlockMaxRange);
                } while (cpuMinerJob.EasyBlockValues.Contains(choseRandom));

                do
                {
                    choseRandom2 = RandomNumberGeneratorUtility.GetBiasRandomBetween(cpuMinerJob.BlockMinRange, cpuMinerJob.BlockMaxRange);
                } while (cpuMinerJob.EasyBlockValues.Contains(choseRandom2));

                DoMathCalculations(threadId, choseRandom, choseRandom2, JobTypeRandom, cpuMinerJob);
                DoMathCalculations(threadId, choseRandom2, choseRandom, JobTypeRandom, cpuMinerJob);

                if (cpuMinerJob.BlockFound) return;
                if (cpuMinerJob.HasNewBlock) return;

                for (var i = cpuMinerJob.EasyBlockValues.Length - 1; i >= 0; i--)
                {
                    var choseRandom3 = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

                    DoMathCalculations(threadId, choseRandom, easyBlockValues.GetRef(choseRandom3), JobTypeSemiRandom, cpuMinerJob);
                    DoMathCalculations(threadId, easyBlockValues.GetRef(choseRandom3), choseRandom, JobTypeSemiRandom, cpuMinerJob);

                    DoMathCalculations(threadId, choseRandom2, easyBlockValues.GetRef(choseRandom3), JobTypeSemiRandom, cpuMinerJob);
                    DoMathCalculations(threadId, easyBlockValues.GetRef(choseRandom3), choseRandom2, JobTypeSemiRandom, cpuMinerJob);

                    (easyBlockValues.GetRef(i), easyBlockValues.GetRef(choseRandom3)) = (easyBlockValues.GetRef(choseRandom3), easyBlockValues.GetRef(i));
                }
            } while (cpuMinerJob is { BlockFound: false, HasNewBlock: false });
        }
        else
        {
            do
            {
                long choseRandom;
                long choseRandom2;

                do
                {
                    choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(cpuMinerJob.BlockMinRange, cpuMinerJob.BlockMaxRange);
                } while (cpuMinerJob.EasyBlockValues.Contains(choseRandom));

                do
                {
                    choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(cpuMinerJob.BlockMinRange, cpuMinerJob.BlockMaxRange);
                } while (cpuMinerJob.EasyBlockValues.Contains(choseRandom2));

                DoMathCalculations(threadId, choseRandom, choseRandom2, JobTypeRandom, cpuMinerJob);
                DoMathCalculations(threadId, choseRandom2, choseRandom, JobTypeRandom, cpuMinerJob);

                if (cpuMinerJob.BlockFound) return;
                if (cpuMinerJob.HasNewBlock) return;

                for (var i = cpuMinerJob.EasyBlockValues.Length - 1; i >= 0; i--)
                {
                    var choseRandom3 = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

                    DoMathCalculations(threadId, choseRandom, easyBlockValues.GetRef(choseRandom3), JobTypeSemiRandom, cpuMinerJob);
                    DoMathCalculations(threadId, easyBlockValues.GetRef(choseRandom3), choseRandom, JobTypeSemiRandom, cpuMinerJob);

                    DoMathCalculations(threadId, choseRandom2, easyBlockValues.GetRef(choseRandom3), JobTypeSemiRandom, cpuMinerJob);
                    DoMathCalculations(threadId, easyBlockValues.GetRef(choseRandom3), choseRandom2, JobTypeSemiRandom, cpuMinerJob);

                    (easyBlockValues.GetRef(i), easyBlockValues.GetRef(choseRandom3)) = (easyBlockValues.GetRef(choseRandom3), easyBlockValues.GetRef(i));
                }
            } while (cpuMinerJob is { BlockFound: false, HasNewBlock: false });
        }
    }

    private void DoMathCalculations(int threadId, long firstNumber, long secondNumber, string jobType, CpuMinerJob cpuMinerJob)
    {
        if (firstNumber > secondNumber)
        {
            // Subtraction Rule:
            var subtractionResult = firstNumber - secondNumber;

            if (subtractionResult >= cpuMinerJob.BlockMinRange)
            {
                ValidateAndSubmitShare(threadId, firstNumber, secondNumber, subtractionResult, '-', jobType, cpuMinerJob);
            }

            // Division Rule:
            var (integerDivideResult, integerDivideRemainder) = Math.DivRem(firstNumber, secondNumber);

            if (integerDivideRemainder == 0 && integerDivideResult >= cpuMinerJob.BlockMinRange)
            {
                ValidateAndSubmitShare(threadId, firstNumber, secondNumber, integerDivideResult, '/', jobType, cpuMinerJob);
            }

            // Modulo Rule:
            if (integerDivideRemainder >= cpuMinerJob.BlockMinRange)
            {
                ValidateAndSubmitShare(threadId, firstNumber, secondNumber, integerDivideRemainder, '%', jobType, cpuMinerJob);
            }
        }
        else
        {
            var integerDivideRemainder = firstNumber % secondNumber;

            // Modulo Rule:
            if (integerDivideRemainder >= cpuMinerJob.BlockMinRange)
            {
                ValidateAndSubmitShare(threadId, firstNumber, secondNumber, integerDivideRemainder, '%', jobType, cpuMinerJob);
            }
        }

        // Addition Rule:
        var additionResult = firstNumber + secondNumber;

        if (additionResult <= cpuMinerJob.BlockMaxRange)
        {
            ValidateAndSubmitShare(threadId, firstNumber, secondNumber, additionResult, '+', jobType, cpuMinerJob);
        }

        // Multiplication Rule:
        var multiplicationResult = firstNumber * secondNumber;

        if (multiplicationResult <= cpuMinerJob.BlockMaxRange)
        {
            ValidateAndSubmitShare(threadId, firstNumber, secondNumber, multiplicationResult, '*', jobType, cpuMinerJob);
        }
    }

    [SkipLocalsInit]
    private void ValidateAndSubmitShare(int threadId, long firstNumber, long secondNumber, long solution, char op, string jobType, CpuMinerJob cpuMinerJob)
    {
        Span<char> stringToEncrypt = stackalloc char[19 + 1 + 1 + 1 + 19 + 19];

        firstNumber.TryFormat(stringToEncrypt, out var firstNumberWritten);

        stringToEncrypt.GetRef(firstNumberWritten) = ' ';
        stringToEncrypt.GetRef(firstNumberWritten + 1) = op;
        stringToEncrypt.GetRef(firstNumberWritten + 2) = ' ';

        secondNumber.TryFormat(stringToEncrypt[(firstNumberWritten + 3)..], out var secondNumberWritten);
        cpuMinerJob.BlockTimestampCreate.TryFormat(stringToEncrypt[(firstNumberWritten + 3 + secondNumberWritten)..], out var finalWritten);

        Span<byte> bytesToEncrypt = stackalloc byte[firstNumberWritten + 3 + secondNumberWritten + finalWritten];
        Encoding.ASCII.GetBytes(stringToEncrypt[..(firstNumberWritten + 3 + secondNumberWritten + finalWritten)], bytesToEncrypt);

        Span<byte> encryptedShare = stackalloc byte[64 * 2];
        Span<byte> hashEncryptedShare = stackalloc byte[64 * 2];

        if (!CpuMinerUtility.MakeEncryptedShare(bytesToEncrypt, encryptedShare, hashEncryptedShare, cpuMinerJob.XorKey, cpuMinerJob.AesKey, cpuMinerJob.AesIv, cpuMinerJob.AesRound))
        {
            return;
        }

        Interlocked.Increment(ref _totalHashCalculatedIn10Seconds.GetRef(threadId));

        Span<char> hashEncryptedShareString = stackalloc char[Encoding.ASCII.GetCharCount(hashEncryptedShare)];
        Encoding.ASCII.GetChars(hashEncryptedShare, hashEncryptedShareString);

        if (!hashEncryptedShareString.SequenceEqual(cpuMinerJob.BlockIndication)) return;

        _network.SendPacketToNetwork(new PacketData($"{NetworkConstants.ReceiveJob}|{Encoding.ASCII.GetString(encryptedShare)}|{solution}|{firstNumber} {op} {secondNumber}|{hashEncryptedShareString}|{cpuMinerJob.BlockHeight}|{_pool.UserAgent}", true, (packet, time) =>
        {
            Span<char> temp = stackalloc char[Encoding.UTF8.GetCharCount(packet)];
            Encoding.UTF8.GetChars(packet, temp);

            if (!temp.StartsWith(NetworkConstants.SendJobStatus)) return;
            temp = temp[(NetworkConstants.SendJobStatus.Length + 1)..];

            if (temp.StartsWith(NetworkConstants.ShareWrong))
            {
                FoundBlock?.Invoke(cpuMinerJob.BlockHeight, jobType, false, InvalidShare, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareUnlock))
            {
                FoundBlock?.Invoke(cpuMinerJob.BlockHeight, jobType, true, string.Empty, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareAleady))
            {
                FoundBlock?.Invoke(cpuMinerJob.BlockHeight, jobType, false, OrphanShare, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareNotExist))
            {
                FoundBlock?.Invoke(cpuMinerJob.BlockHeight, jobType, false, InvalidShare, time.TotalMilliseconds);
            }
        }));

        cpuMinerJob.BlockFound = true;
        Logger.PrintBlockFound(_logger, threadId, jobType, firstNumber, op, secondNumber, solution);
    }

    private void CalculateAverageHashTimerOnElapsed(object? _)
    {
        var captureTime = Stopwatch.GetTimestamp();

        for (var i = AverageHashCalculatedIn10Seconds.Length - 1; i >= 0; i--)
        {
            var capturedValue = Interlocked.Read(ref _totalHashCalculatedIn10Seconds.GetRef(i));
            Interlocked.Exchange(ref _totalHashCalculatedIn10Seconds.GetRef(i), 0);

            _totalHashCalculatedIn60Seconds.GetRef(i) += capturedValue;
            _totalHashCalculatedIn15Minutes.GetRef(i) += capturedValue;

            AverageHashCalculatedIn10Seconds.GetRef(i) = capturedValue / (TimeSpan.FromSeconds(10) + Stopwatch.GetElapsedTime(captureTime)).TotalSeconds;
        }

        _amountSampledFor60Seconds += 10;

        if (_amountSampledFor60Seconds >= 60)
        {
            for (var i = AverageHashCalculatedIn60Seconds.Length - 1; i >= 0; i--)
            {
                AverageHashCalculatedIn60Seconds.GetRef(i) = _totalHashCalculatedIn60Seconds.GetRef(i) / (TimeSpan.FromMinutes(1) + Stopwatch.GetElapsedTime(captureTime)).TotalSeconds;
                _totalHashCalculatedIn60Seconds.GetRef(i) = 0;
            }

            _amountSampledFor60Seconds = 0;
        }

        _amountSampledFor15Minutes += 10;

        if (_amountSampledFor15Minutes >= 60 * 15)
        {
            for (var i = AverageHashCalculatedIn60Seconds.Length - 1; i >= 0; i--)
            {
                AverageHashCalculatedIn15Minutes.GetRef(i) = _totalHashCalculatedIn15Minutes.GetRef(i) / (TimeSpan.FromMinutes(15) + Stopwatch.GetElapsedTime(captureTime)).TotalSeconds;
                _totalHashCalculatedIn15Minutes.GetRef(i) = 0;
            }

            _amountSampledFor15Minutes = 0;
        }
    }
}