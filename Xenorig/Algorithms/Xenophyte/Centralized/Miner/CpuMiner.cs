using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Xenorig.Algorithms.Xenophyte.Centralized.Miner;
using Xenorig.Options;
using Xenorig.Utilities;
using Xenorig.Utilities.KeyDerivationFunction;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal partial class XenophyteCentralizedAlgorithm
{
    private static partial class Native
    {
        [DllImport("libc", EntryPoint = "sched_setaffinity")]
        public static extern int SetThreadAffinityMask_Linux(int pid, int cpuSetSize, in ulong mask);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(long minValue, long maxValue, in long output);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(long minValue, long maxvalue, in long easyBlockValues, int easyBlockValuesLength, in long output);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern bool XenophyteCentralizedAlgorithm_MakeEncryptedShare(in byte input, int inputLength, in byte encryptedShare, in byte hashEncryptedShare, in byte xorKey, int xorKeyLength, int aesKeySize, in byte aesKey, in byte aesIv, int aesRound);
    }

    private const string JobTypeEasy = "Easy Block";
    private const string JobTypeSemiRandom = "Semi Random";
    private const string JobTypeRandom = "Random";

    private const string InvalidShare = "Invalid Share";
    private const string OrphanShare = "Orphan Share";

    private int _isCpuMinerActive;

    private readonly Thread[] _cpuMiningThreads;

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

    private void StartCpuMiner()
    {
        if (Interlocked.CompareExchange(ref _isCpuMinerActive, 1, 0) == 1) return;

        for (var i = 0; i < _cpuMiningThreads.Length; i++)
        {
            var thread = _cpuMiningThreads[i];
            thread.Start();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) continue;

            var threadAffinity = (long) _options.GetCpuMiner().GetThreadAffinity(i);

            if (threadAffinity > 0)
            {
                Process.GetCurrentProcess().Threads[^1].ProcessorAffinity = new IntPtr(threadAffinity);
            }
        }

        _calculateAverageHashTimer.Start();
    }

    private void StopCpuMiner()
    {
        if (Interlocked.CompareExchange(ref _isCpuMinerActive, 0, 1) == 0) return;

        _calculateAverageHashTimer.Stop();
    }

    private void ExecuteCpuMinerThread(int threadId, CpuMiner options)
    {
        try
        {
            Thread.BeginThreadAffinity();

            var threadAffinity = options.GetThreadAffinity(threadId);

            if (threadAffinity != 0)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var _ =Native.SetThreadAffinityMask_Linux(0, sizeof(ulong), threadAffinity);
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

                    try
                    {
                        _blockHeaderLock.EnterReadLock();

                        if (_currentBlockIndication != jobTemplate.BlockIndication)
                        {
                            ref var writableJobTemplate = ref jobTemplate;

                            writableJobTemplate.BlockHeight = _blockId;
                            writableJobTemplate.BlockTimestampCreate = _blockTimestampCreate;
                            writableJobTemplate.BlockIndication = _blockIndication;
                            writableJobTemplate.BlockDifficulty = _blockDifficulty;
                            writableJobTemplate.BlockMinRange = _blockMinRange;
                            writableJobTemplate.BlockMaxRange = _blockMaxRange;

                            var blockXorKeySize = Encoding.ASCII.GetByteCount(_blockXorKey);

                            if (writableJobTemplate.XorKey.Length < blockXorKeySize)
                            {
                                writableJobTemplate.XorKey = new byte[blockXorKeySize];
                            }

                            writableJobTemplate.XorKeyLength = Encoding.ASCII.GetBytes(_blockXorKey, writableJobTemplate.XorKey);

                            var passwordSize = Encoding.ASCII.GetByteCount(_blockKey);

                            if (writableJobTemplate.AesPassword.Length < passwordSize)
                            {
                                writableJobTemplate.AesPassword = new byte[passwordSize];
                            }

                            writableJobTemplate.AesPasswordLength = Encoding.ASCII.GetBytes(_blockKey, writableJobTemplate.AesPassword);

                            var saltSize = Encoding.ASCII.GetByteCount(_blockAesKey);

                            if (writableJobTemplate.AesSalt.Length < saltSize)
                            {
                                writableJobTemplate.AesSalt = new byte[saltSize];
                            }

                            writableJobTemplate.AesSaltLength = Encoding.ASCII.GetBytes(_blockAesKey, writableJobTemplate.AesSalt);

                            if (writableJobTemplate.AesKey.Length < _blockAesSize)
                            {
                                writableJobTemplate.AesKey = new byte[_blockAesSize];
                            }

                            writableJobTemplate.AesKeyLength = _blockAesSize;

                            using (var pbkdf1 = new PBKDF1(writableJobTemplate.AesPassword[..writableJobTemplate.AesPasswordLength], writableJobTemplate.AesSalt[..writableJobTemplate.AesSaltLength]))
                            {
                                pbkdf1.FillBytes(writableJobTemplate.AesKey[..(writableJobTemplate.AesKeyLength / 8)]);
                                pbkdf1.FillBytes(writableJobTemplate.AesIv);
                            }

                            writableJobTemplate.AesRound = _blockAesRound;
                            break;
                        }
                    }
                    finally
                    {
                        _blockHeaderLock.ExitReadLock();
                    }

                    Thread.Sleep(1);
                }

                if (_isCpuMinerActive == 0) break;

                // Received new block!
                DoEasyBlocksCalculations(threadId, options, ref jobTemplate);

                if (_isBlockFound == 1) continue;
                if (_currentBlockIndication != jobTemplate.BlockIndication) continue;

                DoNonEasyBlocksCalculations(threadId, options, ref jobTemplate);
            }
        }
        finally
        {
            Thread.EndThreadAffinity();
        }
    }

    private void DoEasyBlocksCalculations(int threadId, CpuMiner options, ref JobTemplate jobTemplate)
    {
        var numberOfEasyBlockValues = Native.XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(jobTemplate.BlockMinRange, jobTemplate.BlockMaxRange, MemoryMarshal.GetReference(jobTemplate.EasyBlockValues));
        jobTemplate.EasyBlockValuesLength = numberOfEasyBlockValues;

        var (startIndex, size) = GetJobChunk(numberOfEasyBlockValues, options.GetNumberOfThreads(), threadId);
        if (size == 0) return;

        Span<long> temp = stackalloc long[size];
        jobTemplate.EasyBlockValues.Slice(startIndex, size).CopyTo(temp);

        Logger.PrintCurrentThreadJob(_logger, threadId, JobTypeEasy, numberOfEasyBlockValues, startIndex, startIndex + size - 1, size);

        for (var i = size - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = numberOfEasyBlockValues - 1; j >= 0; j--)
            {
                DoMathCalculations(threadId, jobTemplate, temp[choseRandom], jobTemplate.EasyBlockValues[j], JobTypeEasy);
            }

            temp[choseRandom] = temp[i];

            if (_isBlockFound == 1) return;
            if (_currentBlockIndication != jobTemplate.BlockIndication) return;
        }
    }

    private void DoNonEasyBlocksCalculations(int threadId, CpuMiner options, ref JobTemplate jobTemplate)
    {
        var numberOfNonEasyBlockValues = jobTemplate.BlockMaxRange - jobTemplate.BlockMinRange + 1 - jobTemplate.EasyBlockValuesLength;

        if (jobTemplate.NonEasyBlockValues.Length < numberOfNonEasyBlockValues)
        {
            jobTemplate.NonEasyBlockValues = new long[numberOfNonEasyBlockValues];
        }

        jobTemplate.NonEasyBlockValuesLength = Native.XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(jobTemplate.BlockMinRange, jobTemplate.BlockMaxRange, MemoryMarshal.GetReference(jobTemplate.EasyBlockValues), jobTemplate.EasyBlockValuesLength, MemoryMarshal.GetReference(jobTemplate.NonEasyBlockValues));

        var (startIndex, size) = GetJobChunk((int) numberOfNonEasyBlockValues, options.GetNumberOfThreads(), threadId);
        if (size == 0) return;

        Span<long> temp = stackalloc long[size];
        jobTemplate.NonEasyBlockValues.Slice(startIndex, size).CopyTo(temp);

        Logger.PrintCurrentThreadJob(_logger, threadId, JobTypeSemiRandom, numberOfNonEasyBlockValues, startIndex, startIndex + size - 1, size);

        for (var i = size - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = jobTemplate.EasyBlockValuesLength - 1; j >= 0; j--)
            {
                DoMathCalculations(threadId, jobTemplate, temp[choseRandom], jobTemplate.EasyBlockValues[j], JobTypeSemiRandom);
                DoMathCalculations(threadId, jobTemplate, jobTemplate.EasyBlockValues[j], temp[choseRandom], JobTypeSemiRandom);
            }

            (temp[choseRandom], temp[i]) = (temp[i], temp[choseRandom]);

            if (_isBlockFound == 1) break;
            if (_currentBlockIndication != jobTemplate.BlockIndication) break;
        }

        if (_isBlockFound == 1) return;
        if (_currentBlockIndication != jobTemplate.BlockIndication) return;

        Logger.PrintCurrentThreadJob(_logger, threadId, JobTypeRandom, numberOfNonEasyBlockValues, startIndex, startIndex + size - 1, size);

        for (var i = size - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = jobTemplate.NonEasyBlockValuesLength - 1; j >= 0; j--)
            {
                DoMathCalculations(threadId, jobTemplate, temp[choseRandom], jobTemplate.NonEasyBlockValues[j], JobTypeRandom);
                DoMathCalculations(threadId, jobTemplate, jobTemplate.NonEasyBlockValues[j], temp[choseRandom], JobTypeRandom);
            }

            (temp[choseRandom], temp[i]) = (temp[i], temp[choseRandom]);

            if (_isBlockFound == 1) break;
            if (_currentBlockIndication != jobTemplate.BlockIndication) break;
        }
    }

    private void DoMathCalculations(int threadId, in JobTemplate jobTemplate, long firstNumber, long secondNumber, string jobType)
    {
        // Addition Rule:
        if (secondNumber <= jobTemplate.BlockMaxRange - firstNumber)
        {
            ValidateAndSubmitShare(threadId, jobTemplate, firstNumber, secondNumber, firstNumber + secondNumber, '+', jobType);
        }

        // Subtraction Rule:
        var subtractionResult = firstNumber - secondNumber;

        if (subtractionResult >= jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, jobTemplate, firstNumber, secondNumber, subtractionResult, '-', jobType);
        }

        // Multiplication Rule:
        if (secondNumber <= jobTemplate.BlockMaxRange / firstNumber)
        {
            ValidateAndSubmitShare(threadId, jobTemplate, firstNumber, secondNumber, firstNumber * secondNumber, '*', jobType);
        }

        // Division Rule:
        var (integerDivideResult, integerDivideRemainder) = Math.DivRem(firstNumber, secondNumber);

        if (integerDivideRemainder == 0 && integerDivideResult >= jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, jobTemplate, firstNumber, secondNumber, integerDivideResult, '/', jobType);
        }

        // Modulo Rule:
        if (integerDivideRemainder >= jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, jobTemplate, firstNumber, secondNumber, integerDivideRemainder, '%', jobType);
        }
    }

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

        if (!Native.XenophyteCentralizedAlgorithm_MakeEncryptedShare(MemoryMarshal.GetReference(bytesToEncrypt), firstNumberWritten + 3 + secondNumberWritten + finalWritten, MemoryMarshal.GetReference(encryptedShare), MemoryMarshal.GetReference(hashEncryptedShare), MemoryMarshal.GetReference(jobTemplate.XorKey), jobTemplate.XorKeyLength, jobTemplate.AesKeyLength, MemoryMarshal.GetReference(jobTemplate.AesKey), MemoryMarshal.GetReference(jobTemplate.AesIv), jobTemplate.AesRound))
        {
            return;
        }

        Interlocked.Increment(ref _totalHashCalculatedIn10Seconds[threadId]);

        Span<char> hashEncryptedShareString = stackalloc char[Encoding.ASCII.GetCharCount(hashEncryptedShare)];
        Encoding.ASCII.GetChars(hashEncryptedShare, hashEncryptedShareString);

        if (!hashEncryptedShareString.SequenceEqual(jobTemplate.BlockIndication)) return;

        SendPacketToNetwork(new PacketData($"{ReceiveJob}|{Encoding.ASCII.GetString(encryptedShare)}|{solution}|{firstNumber} {op} {secondNumber}|{hashEncryptedShareString}|{jobTemplate.BlockHeight}|{_pools[_poolIndex].GetUserAgent()}", true, (packet, time) =>
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
    }
}