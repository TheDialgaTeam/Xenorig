using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        protected Program Program { get; }

        protected LoggerService LoggerService { get; }

        protected ConfigService ConfigService { get; }

        protected MiningService MiningService { get; }

        protected ICryptoTransform CryptoTransform { get; set; }

        protected HashAlgorithm HashAlgorithm { get; }

        private ConcurrentDictionary<int, decimal> TotalAverage10SecondsHashesCalculated { get; } = new ConcurrentDictionary<int, decimal>();

        private ConcurrentDictionary<int, decimal> TotalAverage60SecondsHashesCalculated { get; } = new ConcurrentDictionary<int, decimal>();

        private ConcurrentDictionary<int, decimal> TotalAverage15MinutesHashesCalculated { get; } = new ConcurrentDictionary<int, decimal>();

        private List<Thread> JobThreads { get; } = new List<Thread>();

        protected AbstractMiner(Program program, LoggerService loggerService, ConfigService configService, MiningService miningService)
        {
            Program = program;
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

        protected async Task WaitForNextJobAsync(string currentJob)
        {
            var cancellationTokenSource = Program.CancellationTokenSource;

            while (currentJob == JobIndication)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    break;

                await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        protected async Task WaitForNextBlockAsync(string currentBlock)
        {
            var cancellationTokenSource = Program.CancellationTokenSource;

            while (currentBlock == BlockIndication)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    break;

                await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        protected abstract Task DoRandomJobAsync(int threadIndex, Config.MiningThread miningThread);

        protected string MakeEncryptedShare(string calculation, int threadIndex)
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

        private void StartAverage10SecondsHashesCalculatedTask()
        {
            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;
                var totalAverageHashesCalculated = TotalAverage10SecondsHashesCalculated;
                var averageHashesCalculated = Average10SecondsHashesCalculated;
                var stopWatch = new Stopwatch();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var totalAverageHashesCalculatedCount = totalAverageHashesCalculated.Count;

                    for (var i = 0; i < totalAverageHashesCalculatedCount; i++)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            break;

                        averageHashesCalculated[i] = totalAverageHashesCalculated[i] / (stopWatch.ElapsedMilliseconds / 1000m);
                        totalAverageHashesCalculated[i] = 0;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                        break;

                    stopWatch.Restart();

                    await Task.Delay(new TimeSpan(0, 0, 10), cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap());
        }

        private void StartAverage60SecondsHashesCalculatedTask()
        {
            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;
                var totalAverageHashesCalculated = TotalAverage60SecondsHashesCalculated;
                var averageHashesCalculated = Average60SecondsHashesCalculated;
                var stopWatch = new Stopwatch();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var totalAverageHashesCalculatedCount = totalAverageHashesCalculated.Count;

                    for (var i = 0; i < totalAverageHashesCalculatedCount; i++)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            break;

                        averageHashesCalculated[i] = totalAverageHashesCalculated[i] / (stopWatch.ElapsedMilliseconds / 1000m);
                        totalAverageHashesCalculated[i] = 0;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                        break;

                    stopWatch.Restart();

                    await Task.Delay(new TimeSpan(0, 1, 0), cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap());
        }

        private void StartAverage15MinutesHashesCalculatedTask()
        {
            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;
                var totalAverageHashesCalculated = TotalAverage15MinutesHashesCalculated;
                var averageHashesCalculated = Average15MinutesHashesCalculated;
                var stopWatch = new Stopwatch();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var totalAverageHashesCalculatedCount = totalAverageHashesCalculated.Count;

                    for (var i = 0; i < totalAverageHashesCalculatedCount; i++)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            break;

                        averageHashesCalculated[i] = totalAverageHashesCalculated[i] / (stopWatch.ElapsedMilliseconds / 1000m);
                        totalAverageHashesCalculated[i] = 0;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                        break;

                    stopWatch.Restart();

                    await Task.Delay(new TimeSpan(0, 15, 0), cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap());
        }

        private void StartCalculateHashesTask()
        {
            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
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

                    await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                        .Write("speed 10s/60s/15m ", true)
                        .Write($"{average10SecondsSum:F0} ", ConsoleColor.Cyan, false)
                        .Write($"{average60SecondsSum:F0} ", ConsoleColor.DarkCyan, false)
                        .Write($"{average15MinutesSum:F0} ", ConsoleColor.DarkCyan, false)
                        .Write("H/s ", ConsoleColor.Cyan, false)
                        .Write("max ", false)
                        .WriteLine($"{MaxHashes:F0} H/s", ConsoleColor.Cyan, false)
                        .Build()).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap());
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

        private async Task DoMiningJobAsync(int threadIndex, Config.MiningThread miningThread)
        {
            var cancellationTokenSource = Program.CancellationTokenSource;

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                while (MiningService.Listener == null)
                    await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);

                if (cancellationTokenSource.IsCancellationRequested)
                    return;

                var listener = MiningService.Listener;

                while (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                    await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);

                if (cancellationTokenSource.IsCancellationRequested)
                    return;

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

        public void Dispose()
        {
            CryptoTransform?.Dispose();
            HashAlgorithm?.Dispose();
        }
    }
}