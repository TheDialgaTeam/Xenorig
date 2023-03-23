using System;
using System.Buffers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Config;
using TheDialgaTeam.Xiropht.Xirorig.Utilities;
using Xenophyte_Connector_All.SoloMining;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner;

public class CpuSoloMiner
{
    public event Action<string, object[]>? Log;
    public event Action<string, string, string>? BlockFound;

    public const string JobTypeEasy = "Easy Block";
    public const string JobTypeSemiRandom = "Semi Random";
    public const string JobTypeRandom = "Random";

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

    private const int MaxFloatPrecision = 16777216;
    private const long MaxDoublePrecision = 9007199254740992;

    private readonly MinerThreadConfiguration _minerThreadConfiguration;
    private readonly int _threadId;
    private readonly int _totalThread;
    private readonly string _userAgent;

    private bool _isActive;
    private Thread? _currentThread;

    private CancellationTokenSource? _cancellationTokenSource;

    // CpuSoloMiner Thread Variables.
    private string? _blockId;
    private string? _blockTimestampCreate;
    private string? _blockKey;
    private string? _blockIndication;
    private string? _blockDifficulty;

    private string? _jobMinRange;
    private string? _jobMaxRange;
    private int _jobAesRound;
    private int _jobAesSize;
    private string? _jobAesKey;
    private string? _jobXorKey;

    private bool _isNewBlockAvailable;
    private bool _isBlockFound;

    private ICryptoTransform? _aesCryptoTransform;

    private long _totalHashCalculatedIn10Seconds;
    private long _totalHashCalculatedIn60Seconds;
    private long _totalHashCalculatedIn15Minutes;

    public CpuSoloMiner(MinerThreadConfiguration minerThreadConfiguration, int threadId, int totalThread)
    {
        _minerThreadConfiguration = minerThreadConfiguration;
        _threadId = threadId;
        _totalThread = totalThread;
        _userAgent = $"Xirorig/{Assembly.GetExecutingAssembly().GetName().Version}";
    }

    private static unsafe void RunThread(object? state)
    {
        if (state is not CpuSoloMiner cpuSoloMiner) return;

        var minerThreadConfiguration = cpuSoloMiner._minerThreadConfiguration;
        var threadId = cpuSoloMiner._threadId;
        var totalThread = cpuSoloMiner._totalThread;
        var cancellationToken = cpuSoloMiner._cancellationTokenSource!.Token;

        var arrayPoolInt = ArrayPool<int>.Shared;
        var arrayPoolLong = ArrayPool<long>.Shared;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Wait for new block.
            while (!cpuSoloMiner._isNewBlockAvailable)
            {
                if (cancellationToken.IsCancellationRequested) break;
                Thread.Sleep(1);
            }

            if (cancellationToken.IsCancellationRequested) break;

            // Received new block!
            cpuSoloMiner._isNewBlockAvailable = false;
            cpuSoloMiner._isBlockFound = false;

            using (var pdb = new PasswordDeriveBytes(cpuSoloMiner._blockKey!, Encoding.UTF8.GetBytes(cpuSoloMiner._jobAesKey!)))
            {
#pragma warning disable 618
                var aes = new RijndaelManaged { BlockSize = cpuSoloMiner._jobAesSize, KeySize = cpuSoloMiner._jobAesSize, Key = pdb.GetBytes(cpuSoloMiner._jobAesSize / 8), IV = pdb.GetBytes(cpuSoloMiner._jobAesSize / 8) };
                cpuSoloMiner._aesCryptoTransform = aes.CreateEncryptor();
#pragma warning restore 618
            }

            try
            {
                var jobMinRange = cpuSoloMiner._jobMinRange;
                var jobMaxRange = cpuSoloMiner._jobMaxRange;
                var blockId = cpuSoloMiner._blockId;
                var blockDifficulty = cpuSoloMiner._blockDifficulty;
                var blockIndication = cpuSoloMiner._blockIndication;

                if (int.TryParse(jobMaxRange, out var jobMaxRangeIntValue) && int.TryParse(jobMinRange, out var jobMinRangeIntValue))
                {
                    // This is integer branch where all job range fall within integer limit and thus utilize integer for performance.
                    // Possible range 2-2147483647
                    var easyBlockNumbers = arrayPoolInt.Rent(256);

                    var nonEasyBlockSize = jobMaxRangeIntValue - jobMinRangeIntValue + 1 - 256;
                    var nonEasyBlockNumbers = arrayPoolInt.Rent(nonEasyBlockSize);

                    try
                    {
                        GenerateEasyBlockNumbers(jobMinRangeIntValue, jobMaxRangeIntValue, easyBlockNumbers.AsSpan(0, 256));

                        // DoEasyBlocksCalculations
                        {
                            var (startIndex, size) = GetJobChunk(256, totalThread, threadId);

                            if (size > 0)
                            {
                                var easyBlockValues = arrayPoolInt.Rent(256);
                                var chunkData = arrayPoolInt.Rent(size);

                                try
                                {
                                    Array.Copy(easyBlockNumbers, easyBlockValues, 256);
                                    Array.Copy(easyBlockNumbers, startIndex, chunkData, 0, size);

                                    cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[34;1mThread: {threadIndex,-2} | Job Type: {jobType:l} | Job Total: {jobTotal} | Job Chunk: {startRange}-{endRange} ({size})\u001b[0m", DateTimeOffset.Now, threadId, JobTypeEasy, 256, startIndex, startIndex + size - 1, size);

                                    for (var i = size - 1; i >= 0; i--)
                                    {
                                        var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

                                        for (var j = 256 - 1; j >= 0; j--)
                                        {
                                            var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                                            DoMathCalculation(cpuSoloMiner, chunkData[choseRandom], easyBlockValues[choseRandom2], jobMinRangeIntValue, jobMaxRangeIntValue, JobTypeEasy, blockId!);

                                            (easyBlockValues[j], easyBlockValues[choseRandom2]) = (easyBlockValues[choseRandom2], easyBlockValues[j]);
                                        }

                                        if (cancellationToken.IsCancellationRequested) break;
                                        if (!blockIndication!.Equals(cpuSoloMiner._blockIndication) || cpuSoloMiner._isBlockFound) break;
                                    }
                                }
                                finally
                                {
                                    arrayPoolInt.Return(easyBlockValues);
                                    arrayPoolInt.Return(chunkData);
                                }
                            }
                        }

                        if (cancellationToken.IsCancellationRequested) break;
                        if (minerThreadConfiguration.EasyBlockOnly) continue;
                        if (!blockIndication!.Equals(cpuSoloMiner._blockIndication) || cpuSoloMiner._isBlockFound) continue;

                        GenerateNonEasyBlockNumbers(jobMinRangeIntValue, jobMaxRangeIntValue, easyBlockNumbers, nonEasyBlockNumbers);

                        // DoNonEasyBlocksCalculations
                        {
                            var (startIndex, size) = GetJobChunk(nonEasyBlockSize, totalThread, threadId);

                            if (size > 0)
                            {
                                var tempNonEasyBlockValues = arrayPoolInt.Rent(size);

                                try
                                {
                                    Array.Copy(nonEasyBlockNumbers, startIndex, tempNonEasyBlockValues, 0, size);

                                    cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[34;1mThread: {threadIndex,-2} | Job Type: {jobType:l} | Job Total: {jobTotal} | Job Chunk: {startRange}-{endRange} ({size})\u001b[0m", DateTimeOffset.Now, threadId, JobTypeSemiRandom, nonEasyBlockSize, startIndex, startIndex + size - 1, size);

                                    for (var i = size - 1; i >= 0; i--)
                                    {
                                        var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

                                        for (var j = easyBlockNumbers.Length - 1; j >= 0; j--)
                                        {
                                            var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                                            DoMathCalculation(cpuSoloMiner, tempNonEasyBlockValues[choseRandom], easyBlockNumbers[choseRandom2], jobMinRangeIntValue, jobMaxRangeIntValue, JobTypeSemiRandom, blockId!);
                                            DoMathCalculation(cpuSoloMiner, easyBlockNumbers[choseRandom2], tempNonEasyBlockValues[choseRandom], jobMinRangeIntValue, jobMaxRangeIntValue, JobTypeSemiRandom, blockId!);

                                            (easyBlockNumbers[j], easyBlockNumbers[choseRandom2]) = (easyBlockNumbers[choseRandom2], easyBlockNumbers[j]);
                                        }

                                        (tempNonEasyBlockValues[choseRandom], tempNonEasyBlockValues[i]) = (tempNonEasyBlockValues[i], tempNonEasyBlockValues[choseRandom]);

                                        if (cancellationToken.IsCancellationRequested) break;
                                        if (!blockIndication.Equals(cpuSoloMiner._blockIndication) || cpuSoloMiner._isBlockFound) break;
                                    }

                                    if (cancellationToken.IsCancellationRequested) break;
                                    if (!blockIndication.Equals(cpuSoloMiner._blockIndication) || cpuSoloMiner._isBlockFound) continue;

                                    cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[34;1mThread: {threadIndex,-2} | Job Type: {jobType:l} | Job Total: {jobTotal} | Job Chunk: {startRange}-{endRange} ({size})\u001b[0m", DateTimeOffset.Now, threadId, JobTypeRandom, nonEasyBlockSize, startIndex, startIndex + size - 1, size);

                                    for (var i = size - 1; i >= 0; i--)
                                    {
                                        var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

                                        for (var j = nonEasyBlockSize - 1; j >= 0; j--)
                                        {
                                            var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                                            DoMathCalculation(cpuSoloMiner, tempNonEasyBlockValues[choseRandom], nonEasyBlockNumbers[choseRandom2], jobMinRangeIntValue, jobMaxRangeIntValue, JobTypeRandom, blockId!);

                                            (nonEasyBlockNumbers[j], nonEasyBlockNumbers[choseRandom2]) = (nonEasyBlockNumbers[choseRandom2], nonEasyBlockNumbers[j]);

                                            if (cancellationToken.IsCancellationRequested) break;
                                            if (!blockIndication.Equals(cpuSoloMiner._blockIndication) || cpuSoloMiner._isBlockFound) break;
                                        }

                                        (tempNonEasyBlockValues[choseRandom], tempNonEasyBlockValues[i]) = (tempNonEasyBlockValues[i], tempNonEasyBlockValues[choseRandom]);

                                        if (cancellationToken.IsCancellationRequested) break;
                                        if (!blockIndication.Equals(cpuSoloMiner._blockIndication) || cpuSoloMiner._isBlockFound) break;
                                    }
                                }
                                finally
                                {
                                    arrayPoolInt.Return(tempNonEasyBlockValues);
                                }
                            }
                        }
                    }
                    finally
                    {
                        arrayPoolInt.Return(easyBlockNumbers);
                        arrayPoolInt.Return(nonEasyBlockNumbers);
                    }
                }
                else if (long.TryParse(jobMaxRange, out var jobMaxRangeLongValue) && long.TryParse(jobMinRange, out var jobMinRangeLongValue))
                {
                    // This is long branch where all job range fall within long limit and thus utilize long for performance.
                    // Possible range 2147483648-9223372036854775807
                    var easyBlockNumbers = arrayPoolLong.Rent(256);

                    try
                    {
                        // Initialize startRange, endRange, easyBlockNumbers
                        GenerateEasyBlockNumbers(jobMinRangeLongValue, jobMaxRangeLongValue, easyBlockNumbers);

                        if (threadId == 0)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[34;1mThread: {threadIndex,-2} | Job Type: {jobType:l} | Job Difficulty: {JobDifficulty:l} | Job Range: {startRange}-{endRange}\u001b[0m", DateTimeOffset.Now, threadId, JobTypeEasy, blockDifficulty!, jobMinRangeLongValue, jobMaxRangeLongValue);

                            fixed (long* easyBlockNumbersPtr = easyBlockNumbers)
                            {
                                var easyBlockNumbersLongPtrI = easyBlockNumbersPtr + 255;

                                for (var i = 255; i >= 0; i--)
                                {
                                    if (cancellationToken.IsCancellationRequested) break;
                                    var easyBlockNumbersLongPtrJ = easyBlockNumbersPtr + 255;

                                    for (var j = 255; j >= 0; j--)
                                    {
                                        DoMathCalculation(cpuSoloMiner, *easyBlockNumbersLongPtrI, *easyBlockNumbersLongPtrJ, jobMinRangeLongValue, jobMaxRangeLongValue, JobTypeEasy, blockId!);
                                        easyBlockNumbersLongPtrJ--;
                                    }

                                    easyBlockNumbersLongPtrI--;
                                }
                            }
                        }

                        if (cancellationToken.IsCancellationRequested) break;
                        if (minerThreadConfiguration.EasyBlockOnly) continue;
                        cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[34;1mThread: {threadIndex,-2} | Job Type: {jobType:l} | Job Difficulty: {JobDifficulty:l} | Job Range: {startRange}-{endRange}\u001b[0m", DateTimeOffset.Now, threadId, JobTypeRandom, blockDifficulty!, jobMinRangeLongValue, jobMaxRangeLongValue);

                        while (blockIndication!.Equals(cpuSoloMiner._blockIndication) && !cpuSoloMiner._isBlockFound)
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            var firstNumber = RandomNumberGeneratorUtility.GetRandomBetween(jobMinRangeLongValue, jobMaxRangeLongValue);
                            var secondNumber = RandomNumberGeneratorUtility.GetRandomBetween(jobMinRangeLongValue, jobMaxRangeLongValue);

                            DoMathCalculation(cpuSoloMiner, firstNumber, secondNumber, jobMinRangeLongValue, jobMaxRangeLongValue, JobTypeRandom, blockId!);
                            DoMathCalculation(cpuSoloMiner, secondNumber, firstNumber, jobMinRangeLongValue, jobMaxRangeIntValue, JobTypeRandom, blockId!);

                            fixed (long* easyBlockNumbersPtr = easyBlockNumbers)
                            {
                                var easyBlockNumbersLongPtr = easyBlockNumbersPtr + 255;

                                for (var i = 255; i >= 0; i--)
                                {
                                    var easyBlockNumber = *easyBlockNumbersLongPtr;

                                    DoMathCalculation(cpuSoloMiner, firstNumber, easyBlockNumber, jobMinRangeLongValue, jobMaxRangeLongValue, JobTypeSemiRandom, blockId!);
                                    DoMathCalculation(cpuSoloMiner, easyBlockNumber, firstNumber, jobMinRangeLongValue, jobMaxRangeLongValue, JobTypeSemiRandom, blockId!);

                                    DoMathCalculation(cpuSoloMiner, secondNumber, easyBlockNumber, jobMinRangeLongValue, jobMaxRangeLongValue, JobTypeSemiRandom, blockId!);
                                    DoMathCalculation(cpuSoloMiner, easyBlockNumber, secondNumber, jobMinRangeLongValue, jobMaxRangeLongValue, JobTypeSemiRandom, blockId!);

                                    easyBlockNumbersLongPtr--;
                                }
                            }
                        }
                    }
                    finally
                    {
                        arrayPoolLong.Return(easyBlockNumbers);
                    }
                }
                else
                {
                    // This is decimal branch, the final fall back that is slow but at least it works.
                    // Possible Range: 9223372036854775808-79228162514264337593543950335
                    // TBH, Xiropht difficulty won't even reach this far before the next fork. So why bother.
                    cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[34;1mThread: {threadIndex,-2} | Job Type: {jobType:l} | Job Difficulty: {JobDifficulty:l} | Job Range: {startRange:l}-{endRange:l}\u001b[0m", DateTimeOffset.Now, threadId, JobTypeRandom, cpuSoloMiner._blockDifficulty!, cpuSoloMiner._jobMinRange!, cpuSoloMiner._jobMaxRange!);
                    cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[31;1mThread: {threadIndex,-2} | Xirorig is not implemented to do this range of number. Skipping to next block.\u001b[0m", DateTimeOffset.Now, threadId);
                }
            }
            finally
            {
                cpuSoloMiner._aesCryptoTransform.Dispose();
            }
        }
    }

    private static (int startIndex, int size) GetJobChunk(int totalSize, int numberOfChunks, int threadId)
    {
        var (quotient, remainder) = Math.DivRem(totalSize, numberOfChunks);
        var startIndex = threadId * quotient + Math.Min(threadId, remainder);
        var nextIndex = (threadId + 1) * quotient + Math.Min(threadId + 1, remainder);
        return (startIndex, nextIndex - startIndex);
    }

    private static void GenerateEasyBlockNumbers(int minValue, int maxValue, Span<int> output)
    {
        var range = maxValue - minValue + 1;

        if (range <= 256)
        {
            var index = 255;

            for (var i = maxValue; i >= minValue; i--)
            {
                output[index--] = i;
            }
        }
        else
        {
            if (range <= MaxFloatPrecision)
            {
                for (var i = 255; i >= 0; i--)
                {
                    output.GetRef(i) = minValue + (int) (Math.Max(0, i / 255.0f - 0.0000001f) * range);
                }
            }
            else
            {
                for (var i = 255; i >= 0; i--)
                {
                    output.GetRef(i) = minValue + (int) (Math.Max(0, i / 255.0 - 0.00000000001) * range);
                }
            }
        }
    }

    private static void GenerateEasyBlockNumbers(long minValue, long maxValue, Span<long> output)
    {
        var range = maxValue - minValue + 1;

        if (range <= 256)
        {
            var index = 255;

            for (var i = maxValue; i >= minValue; i--)
            {
                output[index--] = i;
            }
        }
        else
        {
            for (var i = 255; i >= 0; i--)
            {
                output.GetRef(i) = minValue + (long) (Math.Max(0, i / 255.0 - 0.00000000001) * range);
            }
        }
    }

    private static void GenerateNonEasyBlockNumbers(int minValue, int maxValue, ReadOnlySpan<int> easyBlockValues, Span<int> output)
    {
        var range = maxValue - minValue + 1 - easyBlockValues.Length;

        if (range is 0 or >= int.MaxValue) return;

        var amount = 0;
        var easyBlockValuesIndex = 0;

        for (var i = minValue; i <= maxValue; i++)
        {
            if (i < easyBlockValues[easyBlockValuesIndex])
            {
                output.GetRef(amount++) = i;
            }
            else
            {
                easyBlockValuesIndex++;
            }
        }
    }

    private static void GenerateNonEasyBlockNumbers(long minValue, long maxValue, ReadOnlySpan<long> easyBlockValues, Span<long> output)
    {
        var range = maxValue - minValue + 1 - easyBlockValues.Length;

        if (range is 0 or >= int.MaxValue) return;

        var amount = 0;
        var easyBlockValuesIndex = 0;

        for (var i = minValue; i <= maxValue; i++)
        {
            if (i < easyBlockValues[easyBlockValuesIndex])
            {
                output[amount++] = i;
            }
            else
            {
                easyBlockValuesIndex++;
            }
        }
    }

    private static void DoMathCalculation(CpuSoloMiner cpuSoloMiner, int firstNumber, int secondNumber, int jobMinRange, int jobMaxRange, string jobType, string blockId)
    {
        // Addition Rule:
        var additionResult = firstNumber + secondNumber;

        if (additionResult <= jobMaxRange)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, additionResult, "+", jobType, blockId);
        }

        // Subtraction Rule:
        var subtractionResult = firstNumber - secondNumber;

        if (subtractionResult >= jobMinRange)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, subtractionResult, "-", jobType, blockId);
        }

        // Multiplication Rule:
        var multiplicationResult = firstNumber * secondNumber;

        if (multiplicationResult <= jobMaxRange)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, multiplicationResult, "*", jobType, blockId);
        }

        // Division Rule:
        var integerDivideResult = Math.DivRem(firstNumber, secondNumber, out var integerDivideRemainder);

        if (integerDivideRemainder == 0 && integerDivideResult >= jobMinRange)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, integerDivideResult, "/", jobType, blockId);
        }

        // Modulo Rule:
        if (integerDivideRemainder >= jobMinRange)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, integerDivideRemainder, "%", jobType, blockId);
        }
    }

    private static void DoMathCalculation(CpuSoloMiner cpuSoloMiner, long firstNumber, long secondNumber, long jobMinRange, long jobMaxRange, string jobType, string blockId)
    {
        // Addition Rule:
        if (secondNumber <= jobMaxRange - firstNumber)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, firstNumber + secondNumber, "+", jobType, blockId);
        }

        // Subtraction Rule:
        var subtractionResult = firstNumber - secondNumber;

        if (subtractionResult >= jobMinRange)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, subtractionResult, "-", jobType, blockId);
        }

        // Multiplication Rule:
        if (secondNumber <= jobMaxRange / firstNumber)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, firstNumber * secondNumber, "*", jobType, blockId);
        }

        // Division Rule:
        var longDivideResult = Math.DivRem(firstNumber, secondNumber, out var longDivideRemainder);

        if (longDivideRemainder == 0 && longDivideResult >= jobMinRange)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, longDivideResult, "/", jobType, blockId);
        }

        // Modulo Rule:
        if (longDivideRemainder >= jobMinRange)
        {
            ValidateAndSubmitShare(cpuSoloMiner, firstNumber, secondNumber, longDivideRemainder, "%", jobType, blockId);
        }
    }

    private static void ValidateAndSubmitShare(CpuSoloMiner cpuSoloMiner, int firstNumber, int secondNumber, int result, string operatorSymbol, string jobType, string blockId)
    {
        var encryptedShare = MiningUtility.MakeEncryptedShare($"{firstNumber} {operatorSymbol} {secondNumber}{cpuSoloMiner._blockTimestampCreate}", cpuSoloMiner._jobXorKey!, cpuSoloMiner._jobAesRound, cpuSoloMiner._aesCryptoTransform!);
        var hashEncryptedShare = MiningUtility.ComputeHash(encryptedShare);

        Interlocked.Increment(ref cpuSoloMiner._totalHashCalculatedIn10Seconds);
        Interlocked.Increment(ref cpuSoloMiner._totalHashCalculatedIn60Seconds);
        Interlocked.Increment(ref cpuSoloMiner._totalHashCalculatedIn15Minutes);

        if (!hashEncryptedShare.Equals(cpuSoloMiner._blockIndication)) return;

        cpuSoloMiner._isBlockFound = true;
        cpuSoloMiner.SubmitBlock(blockId, jobType, $"{ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob}|{encryptedShare}|{result}|{firstNumber} {operatorSymbol} {secondNumber}|{hashEncryptedShare}|{blockId}|{cpuSoloMiner._userAgent}");
        cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[32;1mThread: {threadId,-2} | Job Type: {jobType:l} | Block found: {firstNumber} {operatorSymbol:l} {secondNumber} = {result}\u001b[0m", DateTimeOffset.Now, cpuSoloMiner._threadId, jobType, firstNumber, operatorSymbol, secondNumber, result);
    }

    private static void ValidateAndSubmitShare(CpuSoloMiner cpuSoloMiner, long firstNumber, long secondNumber, long result, string operatorSymbol, string jobType, string blockId)
    {
        var encryptedShare = MiningUtility.MakeEncryptedShare($"{firstNumber} {operatorSymbol} {secondNumber}{cpuSoloMiner._blockTimestampCreate}", cpuSoloMiner._jobXorKey!, cpuSoloMiner._jobAesRound, cpuSoloMiner._aesCryptoTransform!);
        var hashEncryptedShare = MiningUtility.ComputeHash(encryptedShare);

        Interlocked.Increment(ref cpuSoloMiner._totalHashCalculatedIn10Seconds);
        Interlocked.Increment(ref cpuSoloMiner._totalHashCalculatedIn60Seconds);
        Interlocked.Increment(ref cpuSoloMiner._totalHashCalculatedIn15Minutes);

        if (!hashEncryptedShare.Equals(cpuSoloMiner._blockIndication)) return;

        cpuSoloMiner._isBlockFound = true;
        cpuSoloMiner.SubmitBlock(blockId, jobType, $"{ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob}|{encryptedShare}|{result}|{firstNumber} {operatorSymbol} {secondNumber}|{hashEncryptedShare}|{blockId}|{cpuSoloMiner._userAgent}");
        cpuSoloMiner.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[32;1mThread: {threadId,-2} | Job Type: {jobType:l} | Block found: {firstNumber} {operatorSymbol:l} {secondNumber} = {result}\u001b[0m", DateTimeOffset.Now, cpuSoloMiner._threadId, jobType, firstNumber, operatorSymbol, secondNumber, result);
    }

    public void StartMining()
    {
        if (_isActive) return;
        _isActive = true;

        _cancellationTokenSource = new CancellationTokenSource();

        _currentThread = new Thread(RunThread) { IsBackground = true, Priority = _minerThreadConfiguration.ThreadPriority };
        _currentThread.Start(this);
    }

    public void StopMining()
    {
        if (!_isActive) return;
        _isActive = true;

        _cancellationTokenSource!.Cancel();
        _currentThread!.Join();

        _cancellationTokenSource!.Dispose();
    }

    public void UpdateJob(JObject jobObject)
    {
        if (!_isActive) throw new InvalidOperationException("Miner is not active.");

        _blockId = jobObject["ID"]!.Value<string>();
        _blockTimestampCreate = jobObject["TIMESTAMP"]!.Value<string>();
        _blockKey = jobObject["KEY"]!.Value<string>();
        _blockIndication = jobObject["INDICATION"]!.Value<string>();
        _blockDifficulty = jobObject["DIFFICULTY"]!.Value<string>();

        var job = jobObject["JOB"]!.Value<string>().Split(';', StringSplitOptions.RemoveEmptyEntries);

        _jobMinRange = job[0];
        _jobMaxRange = job[1];

        _jobAesRound = jobObject["AESROUND"]!.Value<int>();
        _jobAesSize = jobObject["AESSIZE"]!.Value<int>();
        _jobAesKey = jobObject["AESKEY"]!.Value<string>();
        _jobXorKey = jobObject["XORKEY"]!.Value<string>();

        _isNewBlockAvailable = true;
    }

    private void LogInformation(string message, params object[] args)
    {
        Log?.Invoke(message, args);
    }

    private void SubmitBlock(string blockId, string jobType, string packet)
    {
        BlockFound?.Invoke(blockId, jobType, packet);
    }
}