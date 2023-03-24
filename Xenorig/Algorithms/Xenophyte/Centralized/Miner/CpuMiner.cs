using System;
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

public delegate void BlockSubmitResultHandler(string jobType, bool isGoodBlock, string reason, double roundTripTime);

public sealed partial class CpuMiner
{
    private sealed class CpuMinerJob
    {
        public string CurrentBlockIndication { get; set; } = string.Empty;

        public long[] EasyBlockValues { get; private set; } = Array.Empty<long>();

        public int EasyBlockValuesLength { get; private set; }

        public long[] NonEasyBlockValues { get; private set; } = Array.Empty<long>();

        public long[] TempNonEasyBlockValues { get; private set; } = Array.Empty<long>();

        public int NonEasyBlockValuesLength { get; private set; }

        public void GenerateEasyBlockValues(long minValue, long maxValue)
        {
            var range = Math.Min(256, maxValue - minValue + 1);

            if (EasyBlockValues.Length < range)
            {
                EasyBlockValues = GC.AllocateUninitializedArray<long>(256);
            }

            EasyBlockValuesLength = Native.XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(minValue, maxValue, EasyBlockValues);
        }

        public void GenerateNonEasyBlockValues(long minValue, long maxValue)
        {
            var range = maxValue - minValue + 1;
            var length = range - Math.Min(256, range);

            if (length > int.MaxValue)
            {
                NonEasyBlockValuesLength = 0;
                return;
            }

            if (NonEasyBlockValues.Length < length)
            {
                NonEasyBlockValues = GC.AllocateUninitializedArray<long>((int) length);
            }

            if (TempNonEasyBlockValues.Length < length)
            {
                TempNonEasyBlockValues = GC.AllocateUninitializedArray<long>((int) length);
            }

            NonEasyBlockValuesLength = Native.XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(minValue, maxValue, NonEasyBlockValues, TempNonEasyBlockValues);
        }
    }

    private static partial class Native
    {
        [LibraryImport("libc", EntryPoint = "sched_setaffinity")]
        public static partial int SetThreadAffinityMask_Linux(int pid, int cpuSetSize, in ulong mask);

        [LibraryImport(Program.XenoNativeLibrary)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool XenophyteCentralizedAlgorithm_MakeEncryptedShare(ReadOnlySpan<byte> input, int inputLength, Span<byte> encryptedShare, Span<byte> hashEncryptedShare, ReadOnlySpan<byte> xorKey, int xorKeyLength, int aesKeySize, ReadOnlySpan<byte> aesKey, ReadOnlySpan<byte> aesIv, int aesRound);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(long minValue, long maxValue, Span<long> output);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(long minValue, long maxvalue, Span<long> output, Span<long> output2);

        private static readonly byte[] Base16Characters = Encoding.ASCII.GetBytes(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' });
        
        [SkipLocalsInit]
        public static bool MakeEncryptedShare(ReadOnlySpan<byte> input, int inputLength, Span<byte> encryptedShare, Span<byte> hashEncryptedShare, ReadOnlySpan<byte> xorKey, int xorKeyLength, int aesKeySize, ReadOnlySpan<byte> aesKey, ReadOnlySpan<byte> aesIv, int aesRound)
        {
            void ConvertByteArrayToHex(ReadOnlySpan<byte> input, int inputLength, Span<byte> output, bool withDash)
            {
                if (withDash)
                {
                    for (var i = inputLength - 1; i >= 0; i--)
                    {
                        output.GetRef(3 * i) = Base16Characters.GetRef(input.GetRef(i) >> 4);
                        output.GetRef(3 * i + 1) = Base16Characters.GetRef(input.GetRef(i) & 15);

                        if (i == inputLength - 1) {
                            continue;
                        }

                        output.GetRef(3 * i + 2) = (byte) '-';
                    }
                }
                else
                {
                    for (var i = inputLength - 1; i >= 0; i--) {
                        output.GetRef(2 * i) = Base16Characters.GetRef(input.GetRef(i) >> 4);
                        output.GetRef(2 * i + 1) = Base16Characters.GetRef(input.GetRef(i) & 15);
                    }
                }
            }
            
            void XorAndConvertByteArrayToHex(ReadOnlySpan<byte> input, int inputLength, ReadOnlySpan<byte> xorKey, int xorKeyLength, Span<byte> output, bool withDash)
            {
                if (withDash)
                {
                    for (var i = inputLength - 1; i >= 0; i--)
                    {
                        output.GetRef(3 * i) = (byte) (Base16Characters.GetRef(input.GetRef(i) >> 4) ^ xorKey.GetRef(3 * i % xorKeyLength));
                        output.GetRef(3 * i + 1) = (byte) (Base16Characters.GetRef(input.GetRef(i) & 15) ^ xorKey.GetRef((3 * i + 1) % xorKeyLength));

                        if (i == inputLength - 1) {
                            continue;
                        }

                        output.GetRef(3 * i + 2) = (byte) ((byte) '-' ^ xorKey.GetRef((3 * i + 2) % xorKeyLength));
                    }
                }
                else
                {
                    for (var i = inputLength - 1; i >= 0; i--) {
                        output.GetRef(2 * i) = (byte) (Base16Characters.GetRef(input.GetRef(i) >> 4) ^ xorKey.GetRef(2 * i % xorKeyLength));
                        output.GetRef(2 * i + 1) = (byte) (Base16Characters.GetRef(input.GetRef(i) & 15) ^ xorKey.GetRef((2 * i + 1) % xorKeyLength));
                    }
                }
            }
            
            // First encryption phase convert to hex and xor each result.
            var firstOutputLength = inputLength * 2;
            Span<byte> firstOutput = stackalloc byte[firstOutputLength];
            
            XorAndConvertByteArrayToHex(input, inputLength, xorKey, xorKeyLength, firstOutput, false);
            
            // Second encryption phase: run through aes per round and apply xor at the final round.
            
            var secondInputLength = firstOutputLength;
            var secondInputPaddedLength = firstOutputLength + (16 - firstOutputLength % 16);
            var secondOutputLength = secondInputPaddedLength * 2 + (secondInputPaddedLength - 1);
            
            for (var i = aesRound - 1; i >= 0; i--) {
                var temp = secondOutputLength + (16 - secondOutputLength % 16);
                secondOutputLength = temp * 2 + (temp - 1);
            }

            return true;
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
    private readonly XenorigOptions _options;
    private readonly Network _network;
    private readonly JobTemplate _jobTemplate;

    private int _isCpuMinerActive;

    private readonly Thread[] _cpuMiningThreads;

    private readonly Timer _calculateAverageHashTimer;

    private int _amountSampledFor60Seconds;
    private int _amountSampledFor15Minutes;

    private readonly long[] _totalHashCalculatedIn10Seconds;
    private readonly long[] _totalHashCalculatedIn60Seconds;
    private readonly long[] _totalHashCalculatedIn15Minutes;

    public CpuMiner(ILogger logger, XenorigOptions options, Network network, JobTemplate jobTemplate)
    {
        _logger = logger;
        _options = options;
        _network = network;
        _jobTemplate = jobTemplate;

        var totalThreads = options.Xenophyte_Centralized_Solo.CpuMiner.GetNumberOfThreads();

        _cpuMiningThreads = new Thread[totalThreads];

        _calculateAverageHashTimer = new Timer(CalculateAverageHashTimerOnElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        AverageHashCalculatedIn10Seconds = new double[totalThreads];
        AverageHashCalculatedIn60Seconds = new double[totalThreads];
        AverageHashCalculatedIn15Minutes = new double[totalThreads];

        _totalHashCalculatedIn10Seconds = new long[totalThreads];
        _totalHashCalculatedIn60Seconds = new long[totalThreads];
        _totalHashCalculatedIn15Minutes = new long[totalThreads];
    }

    private static (int startIndex, int size) GetJobChunk(int totalSize, int numberOfChunks, int threadId)
    {
        var (quotient, remainder) = Math.DivRem(totalSize, numberOfChunks);
        var startIndex = threadId * quotient + Math.Min(threadId, remainder);
        var nextIndex = (threadId + 1) * quotient + Math.Min(threadId + 1, remainder);
        return (startIndex, nextIndex - startIndex);
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

            var thread = _cpuMiningThreads[i];
            thread.Start();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) continue;

            var threadAffinity = (long) _options.Xenophyte_Centralized_Solo.CpuMiner.GetThreadAffinity(i);

            if (threadAffinity > 0)
            {
                Process.GetCurrentProcess().Threads[^1].ProcessorAffinity = new nint(threadAffinity);
            }
        }

        _calculateAverageHashTimer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    public void StopCpuMiner()
    {
        if (Interlocked.CompareExchange(ref _isCpuMinerActive, 0, 1) == 0) return;

        _calculateAverageHashTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private void ExecuteCpuMinerThread(int threadId, Options.CpuMiner options)
    {
        var threadAffinity = options.GetThreadAffinity(threadId);

        if (threadAffinity != 0)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Native.SetThreadAffinityMask_Linux(0, sizeof(ulong), threadAffinity);
            }
        }

        // Thread Variable
        var cpuMinerJob = new CpuMinerJob();

        while (_isCpuMinerActive == 1)
        {
            // Wait for new block.
            do
            {
                if (cpuMinerJob.CurrentBlockIndication != _jobTemplate.BlockIndication)
                {
                    cpuMinerJob.CurrentBlockIndication = _jobTemplate.BlockIndication;
                    break;
                }

                Thread.Sleep(1);
            } while (_isCpuMinerActive == 1);

            if (_isCpuMinerActive == 0) break;

            // Received new block!
            DoEasyBlocksCalculations(threadId, options, cpuMinerJob);

            if (_jobTemplate.BlockFound) continue;
            if (cpuMinerJob.CurrentBlockIndication != _jobTemplate.BlockIndication) continue;

            DoNonEasyBlocksCalculations(threadId, options, cpuMinerJob);
        }
    }

    [SkipLocalsInit]
    private void DoEasyBlocksCalculations(int threadId, Options.CpuMiner options, CpuMinerJob cpuMinerJob)
    {
        cpuMinerJob.GenerateEasyBlockValues(_jobTemplate.BlockMinRange, _jobTemplate.BlockMaxRange);

        var (startIndex, size) = GetJobChunk(cpuMinerJob.EasyBlockValuesLength, options.GetNumberOfThreads(), threadId);
        if (size == 0) return;

        var easyBlockValues = cpuMinerJob.EasyBlockValues.AsSpan();
        Span<long> chunkData = stackalloc long[size];
        
        BufferUtility.MemoryCopy(easyBlockValues.Slice(startIndex, size), chunkData, size);

        Logger.PrintCurrentChunkedThreadJob(_logger, threadId, JobTypeEasy, cpuMinerJob.EasyBlockValuesLength * size, startIndex, startIndex + size - 1, size);

        for (var i = size - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = cpuMinerJob.EasyBlockValuesLength - 1; j >= 0; j--)
            {
                var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                DoMathCalculations(threadId, chunkData.GetRef(choseRandom), easyBlockValues.GetRef(choseRandom2), JobTypeEasy);

                (easyBlockValues.GetRef(j), easyBlockValues.GetRef(choseRandom2)) = (easyBlockValues.GetRef(choseRandom2), easyBlockValues.GetRef(j));
            }
            
            if (_jobTemplate.BlockFound) return;
            if (cpuMinerJob.CurrentBlockIndication != _jobTemplate.BlockIndication) return;

            chunkData.GetRef(choseRandom) = chunkData.GetRef(i);
        }
    }

    private void DoNonEasyBlocksCalculations(int threadId, Options.CpuMiner options, CpuMinerJob cpuMinerJob)
    {
        cpuMinerJob.GenerateNonEasyBlockValues(_jobTemplate.BlockMinRange, _jobTemplate.BlockMaxRange);
        if (cpuMinerJob.NonEasyBlockValuesLength == 0) return;

        var easyBlockValues = cpuMinerJob.EasyBlockValues.AsSpan();
        var nonEasyBlockValues = cpuMinerJob.NonEasyBlockValues.AsSpan();
        
        Logger.PrintCurrentThreadJob(_logger, threadId, JobTypeRandom);

        do
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, cpuMinerJob.NonEasyBlockValuesLength);
            var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, cpuMinerJob.NonEasyBlockValuesLength);
            
            DoMathCalculations(threadId, nonEasyBlockValues.GetRef(choseRandom), nonEasyBlockValues.GetRef(choseRandom2), JobTypeRandom);
            DoMathCalculations(threadId, nonEasyBlockValues.GetRef(choseRandom2), nonEasyBlockValues.GetRef(choseRandom), JobTypeRandom);
            
            if (_jobTemplate.BlockFound) return;
            if (cpuMinerJob.CurrentBlockIndication != _jobTemplate.BlockIndication) return;
            
            for (var i = cpuMinerJob.EasyBlockValuesLength - 1; i >= 0; i--)
            {
                var choseRandom3 = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

                DoMathCalculations(threadId, nonEasyBlockValues.GetRef(choseRandom), easyBlockValues.GetRef(choseRandom3), JobTypeSemiRandom);
                DoMathCalculations(threadId, easyBlockValues.GetRef(choseRandom3), nonEasyBlockValues.GetRef(choseRandom), JobTypeSemiRandom);

                DoMathCalculations(threadId, nonEasyBlockValues.GetRef(choseRandom2), easyBlockValues.GetRef(choseRandom3), JobTypeSemiRandom);
                DoMathCalculations(threadId, easyBlockValues.GetRef(choseRandom3), nonEasyBlockValues.GetRef(choseRandom2), JobTypeSemiRandom);

                (easyBlockValues.GetRef(i), easyBlockValues.GetRef(choseRandom3)) = (easyBlockValues.GetRef(choseRandom3), easyBlockValues.GetRef(i));
            }
        } while (!_jobTemplate.BlockFound && cpuMinerJob.CurrentBlockIndication == _jobTemplate.BlockIndication);
    }

    private void DoMathCalculations(int threadId, long firstNumber, long secondNumber, string jobType)
    {
        // Addition Rule:
        var additionResult = firstNumber + secondNumber;

        if (additionResult <= _jobTemplate.BlockMaxRange)
        {
            ValidateAndSubmitShare(threadId, firstNumber, secondNumber, additionResult, '+', jobType);
        }

        // Subtraction Rule:
        var subtractionResult = firstNumber - secondNumber;

        if (subtractionResult >= _jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, firstNumber, secondNumber, subtractionResult, '-', jobType);
        }

        // Multiplication Rule:
        var multiplicationResult = firstNumber * secondNumber;

        if (multiplicationResult <= _jobTemplate.BlockMaxRange)
        {
            ValidateAndSubmitShare(threadId, firstNumber, secondNumber, multiplicationResult, '*', jobType);
        }

        // Division Rule:
        var (integerDivideResult, integerDivideRemainder) = Math.DivRem(firstNumber, secondNumber);

        if (integerDivideRemainder == 0 && integerDivideResult >= _jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, firstNumber, secondNumber, integerDivideResult, '/', jobType);
        }

        // Modulo Rule:
        if (integerDivideRemainder >= _jobTemplate.BlockMinRange)
        {
            ValidateAndSubmitShare(threadId, firstNumber, secondNumber, integerDivideRemainder, '%', jobType);
        }
    }

    [SkipLocalsInit]
    private void ValidateAndSubmitShare(int threadId, long firstNumber, long secondNumber, long solution, char op, string jobType)
    {
        Span<char> stringToEncrypt = stackalloc char[19 + 1 + 1 + 1 + 19 + 19 + 1];

        firstNumber.TryFormat(stringToEncrypt, out var firstNumberWritten);

        stringToEncrypt.GetRef(firstNumberWritten) = ' ';
        stringToEncrypt.GetRef(firstNumberWritten + 1) = op;
        stringToEncrypt.GetRef(firstNumberWritten + 2) = ' ';

        secondNumber.TryFormat(stringToEncrypt[(firstNumberWritten + 3)..], out var secondNumberWritten);
        _jobTemplate.BlockTimestampCreate.TryFormat(stringToEncrypt[(firstNumberWritten + 3 + secondNumberWritten)..], out var finalWritten);

        Span<byte> bytesToEncrypt = stackalloc byte[firstNumberWritten + 3 + secondNumberWritten + finalWritten];
        Encoding.ASCII.GetBytes(stringToEncrypt[..(firstNumberWritten + 3 + secondNumberWritten + finalWritten)], bytesToEncrypt);

        Span<byte> encryptedShare = stackalloc byte[64 * 2];
        Span<byte> hashEncryptedShare = stackalloc byte[64 * 2];

        if (!Native.XenophyteCentralizedAlgorithm_MakeEncryptedShare(bytesToEncrypt, firstNumberWritten + 3 + secondNumberWritten + finalWritten, encryptedShare, hashEncryptedShare, _jobTemplate.XorKey, _jobTemplate.XorKeyLength, _jobTemplate.AesKeyLength, _jobTemplate.AesKey, _jobTemplate.AesIv, _jobTemplate.AesRound))
        {
            return;
        }

        Interlocked.Increment(ref _totalHashCalculatedIn10Seconds.GetRef(threadId));

        Span<char> hashEncryptedShareString = stackalloc char[Encoding.ASCII.GetCharCount(hashEncryptedShare)];
        Encoding.ASCII.GetChars(hashEncryptedShare, hashEncryptedShareString);

        if (!hashEncryptedShareString.SequenceEqual(_jobTemplate.BlockIndication)) return;

        _network.SendPacketToNetwork(new PacketData($"{NetworkConstants.ReceiveJob}|{Encoding.ASCII.GetString(encryptedShare)}|{solution}|{firstNumber} {op} {secondNumber}|{hashEncryptedShareString}|{_jobTemplate.BlockHeight}|{_network.UserAgent}", true, (packet, time) =>
        {
            Span<char> temp = stackalloc char[Encoding.ASCII.GetCharCount(packet)];
            Encoding.ASCII.GetChars(packet, temp);

            if (!temp.StartsWith(NetworkConstants.SendJobStatus)) return;
            temp = temp[(NetworkConstants.SendJobStatus.Length + 1)..];

            if (temp.StartsWith(NetworkConstants.ShareWrong))
            {
                FoundBlock?.Invoke(jobType, false, InvalidShare, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareUnlock))
            {
                FoundBlock?.Invoke(jobType, true, string.Empty, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareAleady))
            {
                FoundBlock?.Invoke(jobType, false, OrphanShare, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareNotExist))
            {
                FoundBlock?.Invoke(jobType, false, InvalidShare, time.TotalMilliseconds);
            }
        }));

        _jobTemplate.BlockFound = true;
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