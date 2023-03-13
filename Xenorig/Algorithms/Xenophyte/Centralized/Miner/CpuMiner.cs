﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Xenorig.Algorithms.Xenophyte.Centralized.Networking;
using Xenorig.Options;
using Xenorig.Utilities;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Miner;

public sealed partial class CpuMiner
{
    private static partial class Native
    {
        [LibraryImport("libc", EntryPoint = "sched_setaffinity")]
        public static partial int SetThreadAffinityMask_Linux(int pid, int cpuSetSize, in ulong mask);

        [LibraryImport(Program.XenoNativeLibrary)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool XenophyteCentralizedAlgorithm_MakeEncryptedShare(in byte input, int inputLength, ref byte encryptedShare, ref byte hashEncryptedShare, in byte xorKey, int xorKeyLength, int aesKeySize, in byte aesKey, in byte aesIv, int aesRound);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(long minValue, long maxValue, in long output);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(long minValue, long maxvalue, in long easyBlockValues, int easyBlockValuesLength, in long output);
    }

    private const string JobTypeEasy = "Easy Block";
    private const string JobTypeSemiRandom = "Semi Random";
    private const string JobTypeRandom = "Random";

    private const string InvalidShare = "Invalid Share";
    private const string OrphanShare = "Orphan Share";
    
    private readonly ILogger _logger;
    private readonly XenorigOptions _options;
    private readonly Network _network;

    private int _isCpuMinerActive;

    private readonly Thread[] _cpuMiningThreads;
    
    private readonly Timer _calculateAverageHashTimer;
    
    private readonly double[] _averageHashCalculatedIn10Seconds;
    private readonly double[] _averageHashCalculatedIn60Seconds;
    private readonly double[] _averageHashCalculatedIn15Minutes;
    
    private int _amountSampledFor60Seconds;
    private int _amountSampledFor15Minutes;

    private readonly long[] _totalHashCalculatedIn10Seconds;
    private readonly long[] _totalHashCalculatedIn60Seconds;
    private readonly long[] _totalHashCalculatedIn15Minutes;

    private static (int startIndex, int size) GetJobChunk(int totalSize, int numberOfChunks, int threadId)
    {
        var (quotient, remainder) = Math.DivRem(totalSize, numberOfChunks);
        var startIndex = threadId * quotient + Math.Min(threadId, remainder);
        var nextIndex = (threadId + 1) * quotient + Math.Min(threadId + 1, remainder);
        return (startIndex, nextIndex - startIndex);
    }

    public CpuMiner(ILogger logger, XenorigOptions options, Network network)
    {
        _logger = logger;
        _options = options;
        _network = network;
        
        _calculateAverageHashTimer = new Timer(CalculateAverageHashTimerOnElapsed, null, TimeSpan.Zero, TimeSpan.Zero);
    }
    
    private void StartCpuMiner()
    {
        if (Interlocked.CompareExchange(ref _isCpuMinerActive, 1, 0) == 1) return;

        for (var i = 0; i < _cpuMiningThreads.Length; i++)
        {
            var thread = _cpuMiningThreads[i];
            thread.Start();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) continue;

            var threadAffinity = (long) _options.Xenophyte_Centralized_Solo.CpuMiner.GetThreadAffinity(i);

            if (threadAffinity > 0)
            {
                Process.GetCurrentProcess().Threads[^1].ProcessorAffinity = new nint(threadAffinity);
            }
        }

        _calculateAverageHashTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void StopCpuMiner()
    {
        if (Interlocked.CompareExchange(ref _isCpuMinerActive, 0, 1) == 0) return;

        _calculateAverageHashTimer.Change(TimeSpan.Zero, TimeSpan.Zero);
    }

    private void ExecuteCpuMinerThread(int threadId, Options.CpuMiner options)
    {
        try
        {
            Thread.BeginThreadAffinity();

            var threadAffinity = options.GetThreadAffinity(threadId);

            if (threadAffinity != 0)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Native.SetThreadAffinityMask_Linux(0, sizeof(ulong), threadAffinity);
                }
            }

            // Thread Variable
            var jobTemplate = new JobTemplate();

            while (_isCpuMinerActive == 1)
            {
                // Wait for new block.
                while (true)
                {
                    if (_isCpuMinerActive == 0) break;
/*
                    lock (_blockHeaderLock)
                    {
                        if (_currentBlockIndication != jobTemplate.BlockIndication)
                        {
                            ref var writableJobTemplate = ref jobTemplate;

                            writableJobTemplate.BlockHeight = _blockHeight;
                            writableJobTemplate.BlockTimestampCreate = _blockTimestampCreate;
                            writableJobTemplate.BlockIndication = _blockIndication;
                            writableJobTemplate.BlockDifficulty = _blockDifficulty;
                            writableJobTemplate.BlockMinRange = _blockMinRange;
                            writableJobTemplate.BlockMaxRange = _blockMaxRange;
                            writableJobTemplate.XorKey = _blockXorKey.AsSpan(0, _blockXorKeyLength);
                            writableJobTemplate.AesKey = _blockAesKey.AsSpan(0, _blockAesKeySize);
                            writableJobTemplate.AesIv = _blockAesIv;
                            writableJobTemplate.AesRound = _blockAesRound;
                            break;
                        }
                    }
*/
                    Thread.Sleep(1);
                }

                if (_isCpuMinerActive == 0) break;

                // Received new block!
                DoEasyBlocksCalculations(threadId, options, ref jobTemplate);
/*
                if (_isBlockFound == 1) continue;
                if (_currentBlockIndication != jobTemplate.BlockIndication) continue;
*/
                try
                {
                    DoNonEasyBlocksCalculations(threadId, options, ref jobTemplate);
                }
                catch (OutOfMemoryException)
                {
                    jobTemplate.NonEasyBlockValues = Span<long>.Empty;
                    jobTemplate.TempNonEasyBlockValues = Span<long>.Empty;

                    GC.Collect();
                }
            }
        }
        finally
        {
            Thread.EndThreadAffinity();
        }
    }

    [SkipLocalsInit]
    private void DoEasyBlocksCalculations(int threadId, Options.CpuMiner options, ref JobTemplate jobTemplate)
    {
        jobTemplate.EasyBlockValuesLength = Native.XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(jobTemplate.BlockMinRange, jobTemplate.BlockMaxRange, MemoryMarshal.GetReference(jobTemplate.EasyBlockValues));

        var (startIndex, size) = GetJobChunk(jobTemplate.EasyBlockValuesLength, options.GetNumberOfThreads(), threadId);
        if (size == 0) return;

        Span<long> easyBlockValues = stackalloc long[jobTemplate.EasyBlockValuesLength];
        Span<long> chunkData = stackalloc long[size];

        BufferUtility.MemoryCopy(jobTemplate.EasyBlockValues, easyBlockValues, jobTemplate.EasyBlockValuesLength);
        BufferUtility.MemoryCopy(jobTemplate.EasyBlockValues.Slice(startIndex, size), chunkData, size);

        Logger.PrintCurrentThreadJob(_logger, threadId, JobTypeEasy, jobTemplate.EasyBlockValuesLength, startIndex, startIndex + size - 1, size);

        for (var i = size - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = jobTemplate.EasyBlockValuesLength - 1; j >= 0; j--)
            {
                var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                DoMathCalculations(threadId, in jobTemplate, chunkData[choseRandom], easyBlockValues[choseRandom2], JobTypeEasy);

                (easyBlockValues[j], easyBlockValues[choseRandom2]) = (easyBlockValues[choseRandom2], easyBlockValues[j]);
            }

            chunkData[choseRandom] = chunkData[i];
/*
            if (_isBlockFound == 1) return;
            if (_currentBlockIndication != jobTemplate.BlockIndication) return;
*/
        }
    }

    private void DoNonEasyBlocksCalculations(int threadId, Options.CpuMiner options, ref JobTemplate jobTemplate)
    {
        var nonEasyBlockSize = jobTemplate.BlockMaxRange - jobTemplate.BlockMinRange + 1 - jobTemplate.EasyBlockValuesLength;
        if (nonEasyBlockSize > int.MaxValue) return;

        if (jobTemplate.NonEasyBlockValues.Length < nonEasyBlockSize)
        {
            jobTemplate.NonEasyBlockValues = GC.AllocateUninitializedArray<long>((int) nonEasyBlockSize);
        }

        jobTemplate.NonEasyBlockValuesLength = Native.XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(jobTemplate.BlockMinRange, jobTemplate.BlockMaxRange, MemoryMarshal.GetReference(jobTemplate.EasyBlockValues), jobTemplate.EasyBlockValuesLength, MemoryMarshal.GetReference(jobTemplate.NonEasyBlockValues));

        var (startIndex, size) = GetJobChunk(jobTemplate.NonEasyBlockValuesLength, options.GetNumberOfThreads(), threadId);
        if (size == 0) return;

        if (jobTemplate.TempNonEasyBlockValues.Length < size)
        {
            jobTemplate.TempNonEasyBlockValues = GC.AllocateUninitializedArray<long>(size);
        }

        BufferUtility.MemoryCopy(jobTemplate.NonEasyBlockValues.Slice(startIndex, size), jobTemplate.TempNonEasyBlockValues, size);

        Logger.PrintCurrentThreadJob(_logger, threadId, JobTypeSemiRandom, jobTemplate.NonEasyBlockValuesLength, startIndex, startIndex + size - 1, size);

        for (var i = size - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = jobTemplate.EasyBlockValues.Length - 1; j >= 0; j--)
            {
                var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                DoMathCalculations(threadId, in jobTemplate, jobTemplate.TempNonEasyBlockValues[choseRandom], jobTemplate.EasyBlockValues[choseRandom2], JobTypeSemiRandom);
                DoMathCalculations(threadId, in jobTemplate, jobTemplate.EasyBlockValues[choseRandom2], jobTemplate.TempNonEasyBlockValues[choseRandom], JobTypeSemiRandom);

                (jobTemplate.EasyBlockValues[j], jobTemplate.EasyBlockValues[choseRandom2]) = (jobTemplate.EasyBlockValues[choseRandom2], jobTemplate.EasyBlockValues[j]);
            }

            (jobTemplate.TempNonEasyBlockValues[choseRandom], jobTemplate.TempNonEasyBlockValues[i]) = (jobTemplate.TempNonEasyBlockValues[i], jobTemplate.TempNonEasyBlockValues[choseRandom]);
/*
            if (_isBlockFound == 1) break;
            if (_currentBlockIndication != jobTemplate.BlockIndication) break;
*/
        }
/*
        if (_isBlockFound == 1) return;
        if (_currentBlockIndication != jobTemplate.BlockIndication) return;
*/
        Logger.PrintCurrentThreadJob(_logger, threadId, JobTypeRandom, jobTemplate.NonEasyBlockValuesLength, startIndex, startIndex + size - 1, size);

        for (var i = size - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = jobTemplate.NonEasyBlockValuesLength - 1; j >= 0; j--)
            {
                var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                DoMathCalculations(threadId, in jobTemplate, jobTemplate.TempNonEasyBlockValues[choseRandom], jobTemplate.NonEasyBlockValues[choseRandom2], JobTypeRandom);

                (jobTemplate.NonEasyBlockValues[j], jobTemplate.NonEasyBlockValues[choseRandom2]) = (jobTemplate.NonEasyBlockValues[choseRandom2], jobTemplate.NonEasyBlockValues[j]);
/*
                if (_isBlockFound == 1) break;
                if (_currentBlockIndication != jobTemplate.BlockIndication) break;
*/
            }

            (jobTemplate.TempNonEasyBlockValues[choseRandom], jobTemplate.TempNonEasyBlockValues[i]) = (jobTemplate.TempNonEasyBlockValues[i], jobTemplate.TempNonEasyBlockValues[choseRandom]);
/*
            if (_isBlockFound == 1) break;
            if (_currentBlockIndication != jobTemplate.BlockIndication) break;
*/
        }
    }

    private void DoMathCalculations(int threadId, in JobTemplate jobTemplate, long firstNumber, long secondNumber, string jobType)
    {
        // Addition Rule:
        if (secondNumber <= jobTemplate.BlockMaxRange - firstNumber)
        {
            ValidateAndSubmitShare(threadId, in jobTemplate, firstNumber, secondNumber, firstNumber + secondNumber, '+', jobType);
        }

        // Subtraction Rule:
        var subtractionResult = firstNumber - secondNumber;

        if (subtractionResult >= jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, in jobTemplate, firstNumber, secondNumber, subtractionResult, '-', jobType);
        }

        // Multiplication Rule:
        if (secondNumber <= jobTemplate.BlockMaxRange / firstNumber)
        {
            ValidateAndSubmitShare(threadId, in jobTemplate, firstNumber, secondNumber, firstNumber * secondNumber, '*', jobType);
        }

        // Division Rule:
        var (integerDivideResult, integerDivideRemainder) = Math.DivRem(firstNumber, secondNumber);

        if (integerDivideRemainder == 0 && integerDivideResult >= jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, in jobTemplate, firstNumber, secondNumber, integerDivideResult, '/', jobType);
        }

        // Modulo Rule:
        if (integerDivideRemainder >= jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, in jobTemplate, firstNumber, secondNumber, integerDivideRemainder, '%', jobType);
        }
    }

    [SkipLocalsInit]
    private void ValidateAndSubmitShare(int threadId, in JobTemplate jobTemplate, long firstNumber, long secondNumber, long solution, char op, string jobType)
    {
        Span<char> stringToEncrypt = stackalloc char[19 + 1 + 1 + 1 + 19 + 19 + 1];

        firstNumber.TryFormat(stringToEncrypt, out var firstNumberWritten);

        stringToEncrypt[firstNumberWritten] = ' ';
        stringToEncrypt[firstNumberWritten + 1] = op;
        stringToEncrypt[firstNumberWritten + 2] = ' ';

        secondNumber.TryFormat(stringToEncrypt[(firstNumberWritten + 3)..], out var secondNumberWritten);
        jobTemplate.BlockTimestampCreate.TryFormat(stringToEncrypt[(firstNumberWritten + 3 + secondNumberWritten)..], out var finalWritten);

        Span<byte> bytesToEncrypt = stackalloc byte[firstNumberWritten + 3 + secondNumberWritten + finalWritten];
        Encoding.ASCII.GetBytes(stringToEncrypt[..(firstNumberWritten + 3 + secondNumberWritten + finalWritten)], bytesToEncrypt);

        Span<byte> encryptedShare = stackalloc byte[64 * 2];
        Span<byte> hashEncryptedShare = stackalloc byte[64 * 2];

        if (!Native.XenophyteCentralizedAlgorithm_MakeEncryptedShare(in MemoryMarshal.GetReference(bytesToEncrypt), firstNumberWritten + 3 + secondNumberWritten + finalWritten, ref MemoryMarshal.GetReference(encryptedShare), ref MemoryMarshal.GetReference(hashEncryptedShare), in MemoryMarshal.GetReference(jobTemplate.XorKey), jobTemplate.XorKey.Length, jobTemplate.AesKey.Length, in MemoryMarshal.GetReference(jobTemplate.AesKey), in MemoryMarshal.GetReference(jobTemplate.AesIv), jobTemplate.AesRound))
        {
            return;
        }

        Interlocked.Increment(ref _totalHashCalculatedIn10Seconds[threadId]);
        Interlocked.Increment(ref _totalHashCalculatedIn60Seconds[threadId]);
        Interlocked.Increment(ref _totalHashCalculatedIn15Minutes[threadId]);

        Span<char> hashEncryptedShareString = stackalloc char[Encoding.ASCII.GetCharCount(hashEncryptedShare)];
        Encoding.ASCII.GetChars(hashEncryptedShare, hashEncryptedShareString);

        if (!hashEncryptedShareString.SequenceEqual(jobTemplate.BlockIndication)) return;
        /*
        SendPacketToNetwork(new PacketData($"{ReceiveJob}|{Encoding.ASCII.GetString(encryptedShare)}|{solution}|{firstNumber} {op} {secondNumber}|{hashEncryptedShareString}|{jobTemplate.BlockHeight}|{_pools[_poolIndex].UserAgent}", true, (packet, time) =>
        {
            Span<char> temp = stackalloc char[Encoding.ASCII.GetCharCount(packet)];
            Encoding.ASCII.GetChars(packet, temp);

            if (!temp.StartsWith(SendJobStatus)) return;
            temp = temp[(SendJobStatus.Length + 1)..];

            if (temp.StartsWith(ShareWrong))
            {
                switch (jobType)
                {
                    case JobTypeEasy:
                        Interlocked.Increment(ref _totalBadEasyBlocksSubmitted);
                        break;

                    case JobTypeSemiRandom:
                        Interlocked.Increment(ref _totalBadSemiRandomBlocksSubmitted);
                        break;

                    case JobTypeRandom:
                        Interlocked.Increment(ref _totalBadRandomBlocksSubmitted);
                        break;
                }

                Logger.PrintBlockRejectResult(_logger, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted, InvalidShare, time);
            }
            else if (temp.StartsWith(ShareUnlock))
            {
                switch (jobType)
                {
                    case JobTypeEasy:
                        Interlocked.Increment(ref _totalGoodEasyBlocksSubmitted);
                        break;

                    case JobTypeSemiRandom:
                        Interlocked.Increment(ref _totalGoodSemiRandomBlocksSubmitted);
                        break;

                    case JobTypeRandom:
                        Interlocked.Increment(ref _totalGoodRandomBlocksSubmitted);
                        break;
                }

                Logger.PrintBlockAcceptResult(_logger, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted, time);
            }
            else if (temp.StartsWith(ShareAleady))
            {
                switch (jobType)
                {
                    case JobTypeEasy:
                        Interlocked.Increment(ref _totalBadEasyBlocksSubmitted);
                        break;

                    case JobTypeSemiRandom:
                        Interlocked.Increment(ref _totalBadSemiRandomBlocksSubmitted);
                        break;

                    case JobTypeRandom:
                        Interlocked.Increment(ref _totalBadRandomBlocksSubmitted);
                        break;
                }

                Logger.PrintBlockRejectResult(_logger, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted, OrphanShare, time);
            }
            else if (temp.StartsWith(ShareNotExist))
            {
                switch (jobType)
                {
                    case JobTypeEasy:
                        Interlocked.Increment(ref _totalBadEasyBlocksSubmitted);
                        break;

                    case JobTypeSemiRandom:
                        Interlocked.Increment(ref _totalBadSemiRandomBlocksSubmitted);
                        break;

                    case JobTypeRandom:
                        Interlocked.Increment(ref _totalBadRandomBlocksSubmitted);
                        break;
                }

                Logger.PrintBlockRejectResult(_logger, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted, InvalidShare, time);
            }
        }));

        Interlocked.Exchange(ref _isBlockFound, 1);
        Logger.PrintBlockFound(_logger, threadId, jobType, firstNumber, op, secondNumber, solution);
        */
    }
    
    private void CalculateAverageHashTimerOnElapsed(object? _)
    {
        var captureTime = DateTime.Now;
        
        for (var i = _averageHashCalculatedIn10Seconds.GetUpperBound(0); i >= 0; i--)
        {
            _averageHashCalculatedIn10Seconds[i] = Interlocked.Read(ref _totalHashCalculatedIn10Seconds[i]) / (TimeSpan.FromSeconds(10) + (DateTime.Now - captureTime)).TotalSeconds;
            Interlocked.Exchange(ref _totalHashCalculatedIn10Seconds[i], 0);
        }

        if (Interlocked.Add(ref _amountSampledFor60Seconds, 10) >= 60)
        {
            for (var i = _averageHashCalculatedIn60Seconds.GetUpperBound(0); i >= 0; i--)
            {
                _averageHashCalculatedIn60Seconds[i] = Interlocked.Read(ref _totalHashCalculatedIn60Seconds[i]) / (TimeSpan.FromMinutes(1) + (DateTime.Now - captureTime)).TotalSeconds;
                Interlocked.Exchange(ref _totalHashCalculatedIn60Seconds[i], 0);
            }

            Interlocked.Exchange(ref _amountSampledFor60Seconds, 0);
        }

        if (Interlocked.Add(ref _amountSampledFor15Minutes, 10) >= 60 * 15)
        {
            for (var i = _averageHashCalculatedIn60Seconds.GetUpperBound(0); i >= 0; i--)
            {
                _averageHashCalculatedIn15Minutes[i] = Interlocked.Read(ref _totalHashCalculatedIn15Minutes[i]) / (TimeSpan.FromMinutes(15) + (DateTime.Now - captureTime)).TotalSeconds;
                Interlocked.Exchange(ref _totalHashCalculatedIn15Minutes[i], 0);
            }

            Interlocked.Exchange(ref _amountSampledFor15Minutes, 0);
        }
    }
}