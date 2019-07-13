using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TheDialgaTeam.Xiropht.Xirorig.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Mining;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;
using TheDialgaTeam.Xiropht.Xirorig.Setting;
using Xiropht_Connector_All.Utils;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining
{
    public abstract class AbstractMiner : IDisposable
    {
        public decimal BlockId { get; protected set; }

        public string BlockTimestampCreate { get; protected set; }

        public string BlockKey { get; protected set; }

        public string BlockIndication { get; protected set; }

        public decimal BlockDifficulty { get; protected set; }

        public string JobIndication { get; protected set; }

        public decimal JobDifficulty { get; protected set; }

        public decimal JobMinRange { get; protected set; }

        public decimal JobMaxRange { get; protected set; }

        public string JobMethodName { get; protected set; }

        public int JobAesRound { get; protected set; }

        public int JobAesSize { get; protected set; }

        public string JobAesKey { get; protected set; }

        public string JobXorKey { get; protected set; }

        public Dictionary<int, decimal> Average10SecondsHashesCalculated { get; } = new Dictionary<int, decimal>();

        public Dictionary<int, decimal> Average60SecondsHashesCalculated { get; } = new Dictionary<int, decimal>();

        public Dictionary<int, decimal> Average15MinutesHashesCalculated { get; } = new Dictionary<int, decimal>();

        public decimal MaxHashes { get; private set; }

        protected LoggerService LoggerService { get; }

        protected ConfigService ConfigService { get; }

        protected MiningService MiningService { get; }

        protected ICryptoTransform CryptoTransform { get; set; }

        protected HashAlgorithm HashAlgorithm { get; }

        protected ConcurrentDictionary<string, string> SharesSubmitted { get; } = new ConcurrentDictionary<string, string>();

        protected int SharesToFind { get; set; }

        private ConcurrentDictionary<int, decimal> TotalAverage10SecondsHashesCalculated { get; } = new ConcurrentDictionary<int, decimal>();

        private ConcurrentDictionary<int, decimal> TotalAverage60SecondsHashesCalculated { get; } = new ConcurrentDictionary<int, decimal>();

        private ConcurrentDictionary<int, decimal> TotalAverage15MinutesHashesCalculated { get; } = new ConcurrentDictionary<int, decimal>();

        private List<Thread> JobThreads { get; } = new List<Thread>();

        protected AbstractMiner(LoggerService loggerService, ConfigService configService, MiningService miningService)
        {
            LoggerService = loggerService;
            ConfigService = configService;
            MiningService = miningService;

            HashAlgorithm = SHA512.Create();

            StartAverage10SecondsHashesCalculatedTask();
            StartAverage60SecondsHashesCalculatedTask();
            StartAverage15MinutesHashesCalculatedTask();
            StartCalculateHashesTask();
            GenerateMiningThreads();

            loggerService.LogMessage(new ConsoleMessageBuilder()
                .Write("READY (CPU) ", ConsoleColor.Green, true)
                .Write("threads ", false)
                .WriteLine(JobThreads.Count.ToString(), ConsoleColor.Cyan, false)
                .Build());
        }

        public abstract void UpdateJob(string packet);

        protected abstract Task ValidateAndSubmitAsync(decimal firstNumber, decimal secondNumber, char operatorSymbol, string jobType, int threadIndex, decimal result, string encryptedShare, string hashEncryptedShare);

        private void StartAverage10SecondsHashesCalculatedTask()
        {
            Program.TasksToAwait.Add(Task.Run(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;
                var totalAverageHashesCalculated = TotalAverage10SecondsHashesCalculated;
                var averageHashesCalculated = Average10SecondsHashesCalculated;
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var totalAverageHashesCalculatedCount = totalAverageHashesCalculated.Count;
                    
                    for (var i = 0; i < totalAverageHashesCalculatedCount; i++)
                    {
                        if (stopWatch.ElapsedMilliseconds == 0)
                            break;

                        averageHashesCalculated[i] = totalAverageHashesCalculated[i] / (stopWatch.ElapsedMilliseconds / 1000m);
                        totalAverageHashesCalculated[i] = 0;
                    }
                    
                    stopWatch.Restart();
                    await Task.Delay(new TimeSpan(0, 0, 10), cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token));
        }

        private void StartAverage60SecondsHashesCalculatedTask()
        {
            Program.TasksToAwait.Add(Task.Run(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;
                var totalAverageHashesCalculated = TotalAverage60SecondsHashesCalculated;
                var averageHashesCalculated = Average60SecondsHashesCalculated;
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var totalAverageHashesCalculatedCount = totalAverageHashesCalculated.Count;

                    for (var i = 0; i < totalAverageHashesCalculatedCount; i++)
                    {
                        if (stopWatch.ElapsedMilliseconds == 0)
                            break;

                        averageHashesCalculated[i] = totalAverageHashesCalculated[i] / (stopWatch.ElapsedMilliseconds / 1000m);
                        totalAverageHashesCalculated[i] = 0;
                    }

                    stopWatch.Restart();
                    await Task.Delay(new TimeSpan(0, 1, 0), cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token));
        }

        private void StartAverage15MinutesHashesCalculatedTask()
        {
            Program.TasksToAwait.Add(Task.Run(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;
                var totalAverageHashesCalculated = TotalAverage15MinutesHashesCalculated;
                var averageHashesCalculated = Average15MinutesHashesCalculated;
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var totalAverageHashesCalculatedCount = totalAverageHashesCalculated.Count;

                    for (var i = 0; i < totalAverageHashesCalculatedCount; i++)
                    {
                        if (stopWatch.ElapsedMilliseconds == 0)
                            break;

                        averageHashesCalculated[i] = totalAverageHashesCalculated[i] / (stopWatch.ElapsedMilliseconds / 1000m);
                        totalAverageHashesCalculated[i] = 0;
                    }

                    stopWatch.Restart();
                    await Task.Delay(new TimeSpan(0, 15, 0), cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token));
        }

        private void StartCalculateHashesTask()
        {
            Program.TasksToAwait.Add(Task.Run(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;
                var printTime = ConfigService.PrintTime;
                var average10SecondsHashesCalculated = Average10SecondsHashesCalculated;
                var average60SecondsHashesCalculated = Average60SecondsHashesCalculated;
                var average15MinutesHashesCalculated = Average15MinutesHashesCalculated;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(printTime * 1000, cancellationTokenSource.Token).ConfigureAwait(false);

                    decimal average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;
                    var average10SecondsHashesCalculatedCount = average10SecondsHashesCalculated.Count;
                    var average60SecondsHashesCalculatedCount = average60SecondsHashesCalculated.Count;
                    var average15MinutesHashesCalculatedCount = average15MinutesHashesCalculated.Count;

                    for (var i = 0; i < average10SecondsHashesCalculatedCount; i++)
                        average10SecondsSum += average10SecondsHashesCalculated[i];

                    for (var i = 0; i < average60SecondsHashesCalculatedCount; i++)
                        average60SecondsSum += average60SecondsHashesCalculated[i];

                    for (var i = 0; i < average15MinutesHashesCalculatedCount; i++)
                        average15MinutesSum += average15MinutesHashesCalculated[i];

                    MaxHashes = Math.Max(MaxHashes, average10SecondsSum);

                    LoggerService.LogMessage(new ConsoleMessageBuilder()
                        .Write("speed 10s/60s/15m ", true)
                        .Write($"{average10SecondsSum:F0} ", ConsoleColor.Cyan, false)
                        .Write($"{average60SecondsSum:F0} ", ConsoleColor.DarkCyan, false)
                        .Write($"{average15MinutesSum:F0} ", ConsoleColor.DarkCyan, false)
                        .Write("H/s ", ConsoleColor.Cyan, false)
                        .Write("max ", false)
                        .WriteLine($"{MaxHashes:F0} H/s", ConsoleColor.Cyan, false)
                        .Build());
                }
            }, Program.CancellationTokenSource.Token));
        }

        private void GenerateMiningThreads()
        {
            var randomJobThreads = ConfigService.RandomJobThreads;
            var randomJobThreadLength = randomJobThreads.Length;

            var average10SecondsHashesCalculated = Average10SecondsHashesCalculated;
            var average60SecondsHashesCalculated = Average60SecondsHashesCalculated;
            var average15MinutesHashesCalculated = Average15MinutesHashesCalculated;

            var totalAverage10SecondsHashesCalculated = TotalAverage10SecondsHashesCalculated;
            var totalAverage60SecondsHashesCalculated = TotalAverage60SecondsHashesCalculated;
            var totalAverage15MinutesHashesCalculated = TotalAverage15MinutesHashesCalculated;

            var jobThreads = JobThreads;

            for (var i = 0; i < randomJobThreadLength; i++)
            {
                var threadIndex = i;

                average10SecondsHashesCalculated[threadIndex] = 0;
                average60SecondsHashesCalculated[threadIndex] = 0;
                average15MinutesHashesCalculated[threadIndex] = 0;

                totalAverage10SecondsHashesCalculated[threadIndex] = 0;
                totalAverage60SecondsHashesCalculated[threadIndex] = 0;
                totalAverage15MinutesHashesCalculated[threadIndex] = 0;

                var miningThread = randomJobThreads[i];

                var thread = new Thread(async () => await DoMiningJobAsync(threadIndex, miningThread).ConfigureAwait(false)) { IsBackground = true, Priority = miningThread.ThreadPriority };
                thread.Start();

                jobThreads.Add(thread);
            }
        }

        private async Task WaitForNextJobAsync(string currentJob)
        {
            var cancellationTokenSource = Program.CancellationTokenSource;

            while (currentJob == JobIndication)
                await Task.Delay(1, cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private async Task WaitForNextBlockAsync(string currentBlock)
        {
            var cancellationTokenSource = Program.CancellationTokenSource;

            while (currentBlock == BlockIndication)
                await Task.Delay(1, cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private async Task DoMiningJobAsync(int threadIndex, Config.MiningThread miningThread)
        {
            var cancellationTokenSource = Program.CancellationTokenSource;

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                while (MiningService.Listener == null)
                    await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);

                var listener = MiningService.Listener;

                while (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                    await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);

                switch (miningThread.JobType)
                {
                    case Config.MiningJob.RandomJob:
                        await DoRandomJobAsync(threadIndex, miningThread).ConfigureAwait(false);
                        break;

                    case Config.MiningJob.AdditionJob:
                        break;

                    case Config.MiningJob.SubtractionJob:
                        break;

                    case Config.MiningJob.MultiplicationJob:
                        break;

                    case Config.MiningJob.DivisionJob:
                        break;

                    case Config.MiningJob.ModulusJob:
                        break;
                }
            }
        }

        private async Task DoRandomJobAsync(int threadIndex, Config.MiningThread miningThread)
        {
            var cancellationTokenSource = Program.CancellationTokenSource;
            var listener = MiningService.Listener;
            var randomJobThreads = ConfigService.RandomJobThreads;
            var loggerService = LoggerService;

            var miningPrioritySharesThreads = randomJobThreads.Where(a => a.JobType == Config.MiningJob.RandomJob && a.ShareRange && a.MiningPriority == Config.MiningPriority.Shares).ToArray();
            var miningPrioritySharesThreadsLength = miningPrioritySharesThreads.Length;

            var miningPriorityBlockThreads = randomJobThreads.Where(a => a.JobType == Config.MiningJob.RandomJob && a.ShareRange && a.MiningPriority != Config.MiningPriority.Shares).ToArray();
            var miningPriorityBlockThreadsLength = miningPriorityBlockThreads.Length;

            const string jobType = "Random";

            while (listener.ConnectionStatus == ConnectionStatus.Connected && listener.IsLoggedIn)
            {
                while (JobIndication == null)
                {
                    if (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                        break;

                    await Task.Delay(1, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                var jobMinRange = JobMinRange;
                var jobMaxRange = JobMaxRange;
                var blockDifficulty = BlockDifficulty;

                decimal startRange = 0, endRange = 0;

                if (!miningThread.ShareRange)
                {
                    switch (miningThread.MiningPriority)
                    {
                        case Config.MiningPriority.Shares:
                            (startRange, endRange) = MiningUtility.GetJobRangeByPercentage(jobMinRange, jobMaxRange, miningThread.MinMiningRangePercentage, miningThread.MaxMiningRangePercentage);
                            break;

                        case Config.MiningPriority.Normal:
                        case Config.MiningPriority.Block:
                            (startRange, endRange) = MiningUtility.GetJobRangeByPercentage(2, blockDifficulty, miningThread.MinMiningRangePercentage, miningThread.MaxMiningRangePercentage);
                            break;
                    }
                }
                else
                {
                    decimal totalPossibilities;
                    int index;

                    switch (miningThread.MiningPriority)
                    {
                        case Config.MiningPriority.Shares:
                            totalPossibilities = jobMaxRange - jobMinRange + 1;
                            index = 0;

                            for (var i = 0; i < miningPrioritySharesThreadsLength; i++)
                            {
                                if (miningPrioritySharesThreads[i] != miningThread)
                                    continue;

                                index = i;
                            }

                            (startRange, endRange) = MiningUtility.GetJobRange(totalPossibilities, miningPrioritySharesThreadsLength, index, jobMinRange);
                            break;

                        case Config.MiningPriority.Normal:
                        case Config.MiningPriority.Block:
                            totalPossibilities = BlockDifficulty - 2 + 1;
                            index = 0;

                            for (var i = 0; i < miningPriorityBlockThreadsLength; i++)
                            {
                                if (miningPriorityBlockThreads[i] != miningThread)
                                    continue;

                                index = i;
                            }

                            (startRange, endRange) = MiningUtility.GetJobRange(totalPossibilities, miningPriorityBlockThreadsLength, index, 2);
                            break;
                    }
                }

                loggerService.LogMessage($"Thread: {threadIndex + 1} | Job Type: Random | Job Difficulty: {JobDifficulty} | Job Range: {startRange}-{endRange}", ConsoleColor.Blue);

                var currentJobIndication = JobIndication;
                var currentBlockIndication = BlockIndication;
                var sharesSubmitted = SharesSubmitted;
                var sharesToFind = SharesToFind;

                while (currentJobIndication == JobIndication)
                {
                    if (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                        break;

                    // ReSharper disable EqualExpressionComparison
                    var firstNumber = MiningUtility.GetRandomBetween(0, 100) > MiningUtility.GetRandomBetween(0, 100) ? MiningUtility.GenerateNumberMathCalculation(startRange, endRange) : MiningUtility.GetRandomBetweenJob(startRange, endRange);
                    var secondNumber = MiningUtility.GetRandomBetween(0, 100) > MiningUtility.GetRandomBetween(0, 100) ? MiningUtility.GenerateNumberMathCalculation(startRange, endRange) : MiningUtility.GetRandomBetweenJob(startRange, endRange);
                    // ReSharper restore EqualExpressionComparison

                    switch (miningThread.MiningPriority)
                    {
                        case Config.MiningPriority.Shares:
                            if (firstNumber < jobMinRange || firstNumber > jobMaxRange)
                                continue;

                            if (secondNumber < jobMinRange || secondNumber > jobMaxRange)
                                continue;
                            break;

                        case Config.MiningPriority.Normal:
                        case Config.MiningPriority.Block:
                            if (firstNumber < 2 || firstNumber > blockDifficulty)
                                continue;

                            if (secondNumber < 2 || secondNumber > blockDifficulty)
                                continue;
                            break;
                    }

                    foreach (var randomOperator in MiningUtility.RandomOperatorCalculation)
                    {
                        if (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                            break;

                        if (currentJobIndication != JobIndication)
                            break;

                        if (sharesSubmitted.Values.Contains(currentBlockIndication))
                            break;

                        if (sharesSubmitted.Count >= sharesToFind)
                            break;

                        await DoCalculationAsync(firstNumber, secondNumber, randomOperator, jobType, threadIndex).ConfigureAwait(false);
                        await DoCalculationAsync(secondNumber, firstNumber, randomOperator, jobType, threadIndex).ConfigureAwait(false);
                    }

                    if (sharesSubmitted.Values.Contains(currentBlockIndication))
                        break;

                    if (sharesSubmitted.Count >= sharesToFind)
                        break;
                }

                if (sharesSubmitted.Values.Contains(currentBlockIndication))
                    await WaitForNextBlockAsync(currentBlockIndication).ConfigureAwait(false);

                if (sharesSubmitted.Count >= sharesToFind)
                    await WaitForNextJobAsync(currentJobIndication).ConfigureAwait(false);
            }
        }

        private async Task DoCalculationAsync(decimal firstNumber, decimal secondNumber, char operatorSymbol, string jobType, int threadIndex)
        {
            decimal result = 0;

            switch (operatorSymbol)
            {
                case '+':
                    result = firstNumber + secondNumber;
                    break;

                case '-':
                    result = firstNumber - secondNumber;
                    break;

                case '*':
                    result = firstNumber * secondNumber;
                    break;

                case '/':
                    result = firstNumber / secondNumber;

                    if (result - Math.Round(result, 0) != 0)
                        return;
                    break;

                case '%':
                    result = firstNumber % secondNumber;
                    break;
            }

            if (result < 2 || result > BlockDifficulty)
                return;

            var encryptedShare = MakeEncryptedShare($"{firstNumber} {operatorSymbol} {secondNumber}", threadIndex);

            if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                return;

            var hashEncryptedShare = MiningUtility.ComputeHash(HashAlgorithm, encryptedShare);

            await ValidateAndSubmitAsync(firstNumber, secondNumber, operatorSymbol, jobType, threadIndex, result, encryptedShare, hashEncryptedShare).ConfigureAwait(false);
        }

        private string MakeEncryptedShare(string calculation, int threadIndex)
        {
            try
            {
                var encryptedShare = MiningUtility.ConvertStringToHexAndEncryptXorShare(calculation + BlockTimestampCreate, JobXorKey);
                encryptedShare = MiningUtility.EncryptAesShareRoundAndEncryptXorShare(CryptoTransform, encryptedShare, JobAesRound, JobXorKey);
                encryptedShare = MiningUtility.EncryptAesShare(CryptoTransform, encryptedShare);
                encryptedShare = MiningUtility.ComputeHash(HashAlgorithm, encryptedShare);

                TotalAverage10SecondsHashesCalculated[threadIndex]++;
                TotalAverage60SecondsHashesCalculated[threadIndex]++;
                TotalAverage15MinutesHashesCalculated[threadIndex]++;

                return encryptedShare;
            }
            catch
            {
                return ClassAlgoErrorEnumeration.AlgoError;
            }
        }

        public void Dispose()
        {
            CryptoTransform?.Dispose();
            HashAlgorithm?.Dispose();
        }
    }
}