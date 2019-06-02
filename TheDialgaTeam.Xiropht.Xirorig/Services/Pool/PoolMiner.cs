using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Pool.Packet;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;
using Xiropht_Connector_All.Utils;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Pool
{
    public sealed class PoolMiner : IDisposable
    {
        public int BlockId { get; private set; }

        public string BlockTimestampCreate { get; private set; }

        public string BlockKey { get; private set; }

        public string BlockIndication { get; private set; }

        public decimal BlockDifficulty { get; private set; }

        public string JobIndication { get; private set; }

        public decimal JobDifficulty { get; private set; }

        public decimal JobMinRange { get; private set; }

        public decimal JobMaxRange { get; private set; }

        public string JobMethodName { get; private set; }

        public int JobAesRound { get; private set; }

        public int JobAesSize { get; private set; }

        public string JobAesKey { get; private set; }

        public int JobXorKey { get; private set; }

        public string JobEncryptionKey { get; private set; }

        public List<ulong> Average10SecondsHashesCalculated { get; } = new List<ulong>();

        public List<ulong> Average60SecondsHashesCalculated { get; } = new List<ulong>();

        public List<ulong> Average15MinutesHashesCalculated { get; } = new List<ulong>();

        private Program Program { get; }

        private LoggerService LoggerService { get; }

        private ConfigService ConfigService { get; }

        private PoolService PoolService { get; }

        private Task Average10SecondsHashesCalculatedTask { get; set; }

        private Task Average60SecondsHashesCalculatedTask { get; set; }

        private Task Average15MinutesHashesCalculatedTask { get; set; }

        private ConcurrentDictionary<int, ulong> TotalAverage10SecondsHashesCalculated { get; } = new ConcurrentDictionary<int, ulong>();

        private ConcurrentDictionary<int, ulong> TotalAverage60SecondsHashesCalculated { get; } = new ConcurrentDictionary<int, ulong>();

        private ConcurrentDictionary<int, ulong> TotalAverage15MinutesHashesCalculated { get; } = new ConcurrentDictionary<int, ulong>();

        private List<Thread> JobThreads { get; } = new List<Thread>();

        private ConcurrentDictionary<string, bool> SubmittedShares { get; } = new ConcurrentDictionary<string, bool>();

        private byte[] JobAesKeyBytes { get; set; }

        private byte[] JobAesIvBytes { get; set; }

        public PoolMiner(Program program, LoggerService loggerService, ConfigService configService, PoolService poolService)
        {
            Program = program;
            LoggerService = loggerService;
            ConfigService = configService;
            PoolService = poolService;
        }

        public void UpdateJob(string packet)
        {
            var json = JObject.Parse(packet);

            BlockId = int.Parse(json[PoolJobPacket.Block].ToString());
            BlockTimestampCreate = json[PoolJobPacket.BlockTimestampCreate].ToString();
            BlockKey = json[PoolJobPacket.BlockKey].ToString();
            BlockIndication = json[PoolJobPacket.BlockIndication].ToString();
            BlockDifficulty = decimal.Parse(json[PoolJobPacket.BlockDifficulty].ToString());
            JobIndication = json[PoolJobPacket.JobIndication].ToString();
            JobDifficulty = decimal.Parse(json[PoolJobPacket.JobDifficulty].ToString());
            JobMinRange = decimal.Parse(json[PoolJobPacket.JobMinRange].ToString());
            JobMaxRange = decimal.Parse(json[PoolJobPacket.JobMaxRange].ToString());
            JobMethodName = json[PoolJobPacket.JobMethodName].ToString();
            JobAesRound = int.Parse(json[PoolJobPacket.JobMethodAesRound].ToString());
            JobAesSize = int.Parse(json[PoolJobPacket.JobMethodAesSize].ToString());
            JobAesKey = json[PoolJobPacket.JobMethodAesKey].ToString();
            JobXorKey = int.Parse(json[PoolJobPacket.JobMethodXorKey].ToString());
            JobEncryptionKey = json[PoolJobPacket.JobKeyEncryption].ToString();

            using (var pdb = new PasswordDeriveBytes(BlockKey, Encoding.UTF8.GetBytes(JobAesKey)))
            {
                JobAesKeyBytes = pdb.GetBytes(JobAesSize / 8);
                JobAesIvBytes = pdb.GetBytes(JobAesSize / 8);
            }

            SubmittedShares.Clear();
        }

        public void StartMining()
        {
            //            StartMiningTask?.GetAwaiter().GetResult();

            //            StartMiningTask = Task.Factory.StartNew(async () =>
            //            {
            //                while (PoolService.IsLoggedIn && !PoolService.CancellationTokenSource.IsCancellationRequested)
            //                {
            //                    while (JobIndication == null)
            //                    {
            //                        if (!PoolService.IsLoggedIn || PoolService.CancellationTokenSource.IsCancellationRequested)
            //                            break;

            //                        await Task.Delay(1000).ConfigureAwait(false);
            //                    }

            //                    if (!IsThreadAllocated)
            //                    {
            //                        IsThreadAllocated = true;

            //                        for (var i = 0; i < ConfigService.RandomJobThreads.Length; i++)
            //                        {
            //                            var threadIndex = i;

            //                            TotalAverage10SecondsHashesCalculated[threadIndex] = 0;
            //                            TotalAverage60SecondsHashesCalculated[threadIndex] = 0;
            //                            TotalAverage15MinutesHashesCalculated[threadIndex] = 0;

            //                            var miningThread = ConfigService.RandomJobThreads[i];

            //                            if (miningThread.ThreadAffinityToCpu >= 0)
            //                            {
            //#if WIN
            //                                var thread = new DistributedThread(async () => await DoRandomJob(threadIndex, false).ConfigureAwait(false)) { ProcessorAffinity = 1 << miningThread.ThreadAffinityToCpu };
            //                                thread.ManagedThread.IsBackground = true;
            //                                thread.ManagedThread.Priority = miningThread.ThreadPriority;
            //                                thread.Start();
            //                                RandomJobThreads.Add(thread.ManagedThread);
            //#else
            //                                var thread = new Thread(async () => await DoRandomJob(threadIndex, miningThread.PrioritizePoolSharesVsBlock).ConfigureAwait(false)) { IsBackground = true, Priority = miningThread.ThreadPriority };
            //                                thread.Start();
            //                                RandomJobThreads.Add(thread);
            //#endif
            //                            }
            //                            else
            //                            {
            //                                var thread = new Thread(async () => await DoRandomJob(threadIndex, false).ConfigureAwait(false)) { IsBackground = true, Priority = miningThread.ThreadPriority };
            //                                thread.Start();
            //                                RandomJobThreads.Add(thread);
            //                            }
            //                        }
            //                    }


            //                    TotalHashCalculated = 0;

            //                    for (var i = 0; i < HashesCalculated.Count; i++)
            //                    {
            //                        TotalHashCalculated += HashesCalculated[i];
            //                        HashesCalculated[i] = 0;
            //                    }

            //                    await Task.Delay(1000).ConfigureAwait(false);
            //                }

            //                RandomJobThreads.Clear();
            //            }, PoolService.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();
        }

        private void GenerateMiningThreads()
        {
            for (var i = 0; i < ConfigService.RandomJobThreads.Length; i++)
            {
                var threadIndex = i;

                Average10SecondsHashesCalculated[threadIndex] = 0;
                Average60SecondsHashesCalculated[threadIndex] = 0;
                Average15MinutesHashesCalculated[threadIndex] = 0;

                TotalAverage10SecondsHashesCalculated[threadIndex] = 0;
                TotalAverage60SecondsHashesCalculated[threadIndex] = 0;
                TotalAverage15MinutesHashesCalculated[threadIndex] = 0;

                var miningThread = ConfigService.RandomJobThreads[i];

                if (miningThread.ThreadAffinityToCpu >= 0)
                {
#if WIN
                    var thread = new DistributedThread(async () => await DoRandomJob(threadIndex, false).ConfigureAwait(false)) { ProcessorAffinity = 1 << miningThread.ThreadAffinityToCpu };
                    thread.ManagedThread.IsBackground = true;
                    thread.ManagedThread.Priority = miningThread.ThreadPriority;
                    thread.Start();
                    JobThreads.Add(thread.ManagedThread);
#else
                                            var thread = new Thread(async () => await DoRandomJob(threadIndex, miningThread.PrioritizePoolSharesVsBlock).ConfigureAwait(false)) { IsBackground = true, Priority = miningThread.ThreadPriority };
                                            thread.Start();
                                            RandomJobThreads.Add(thread);
#endif
                }
                else
                {
                    var thread = new Thread(async () => await DoRandomJob(threadIndex, false).ConfigureAwait(false)) { IsBackground = true, Priority = miningThread.ThreadPriority };
                    thread.Start();
                    JobThreads.Add(thread);
                }
            }
        }

        private void StartAverage10SecondsHashesCalculatedTask()
        {
            Average10SecondsHashesCalculatedTask = Task.Factory.StartNew(async () =>
            {
                while (!Program.CancellationTokenSource.IsCancellationRequested)
                {
                    foreach (var hashes in TotalAverage10SecondsHashesCalculated)
                    {
                        Average10SecondsHashesCalculated[hashes.Key] = hashes.Value / 10;
                    }

                    await Task.Delay(new TimeSpan(0, 0, 10)).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();
        }

        private void StartAverage60SecondsHashesCalculatedTask()
        {
            Average60SecondsHashesCalculatedTask = Task.Factory.StartNew(async () =>
            {
                while (!Program.CancellationTokenSource.IsCancellationRequested)
                {
                    foreach (var hashes in TotalAverage60SecondsHashesCalculated)
                    {
                        Average60SecondsHashesCalculated[hashes.Key] = hashes.Value / 60;
                    }

                    await Task.Delay(new TimeSpan(0, 1, 0)).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();
        }

        private void StartAverage15MinutesHashesCalculatedTask()
        {
            Average15MinutesHashesCalculatedTask = Task.Factory.StartNew(async () =>
            {
                while (!Program.CancellationTokenSource.IsCancellationRequested)
                {
                    foreach (var hashes in TotalAverage15MinutesHashesCalculated)
                    {
                        Average15MinutesHashesCalculated[hashes.Key] = hashes.Value / (60 * 15);
                    }

                    await Task.Delay(new TimeSpan(0, 15, 0)).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();
        }

        private async Task DoMiningJob(int threadIndex, Config.MiningJob miningJob, Config.MiningPriority miningPriority)
        {
            while (!Program.CancellationTokenSource.IsCancellationRequested)
            {
                // If the pool is not connected or logged in, pause the miner.
                while (!PoolService.IsConnected || !PoolService.IsLoggedIn)
                    await Task.Delay(1000).ConfigureAwait(false);

                var currentJob = JobIndication;
                await WaitForNextPoolJobAsync(currentJob).ConfigureAwait(false);
            }
        }

        private async Task WaitForNextPoolJobAsync(string currentJob)
        {
            while (currentJob == JobIndication)
            {
                if (Program.CancellationTokenSource.IsCancellationRequested)
                    break;

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        private async Task DoRandomJob(int threadIndex, bool prioritizePoolSharesVsBlock)
        {
            while (PoolService.IsLoggedIn && !PoolService.CancellationTokenSource.IsCancellationRequested)
            {
                decimal startRange, endRange;

                if (prioritizePoolSharesVsBlock)
                    (startRange, endRange) = MiningUtility.GetJobRange(JobMaxRange - JobMinRange + 1, ConfigService.RandomJobThreads.Length, threadIndex, JobMinRange);
                else
                    (startRange, endRange) = MiningUtility.GetJobRange(BlockDifficulty - 1, ConfigService.RandomJobThreads.Length, threadIndex, 2);

                var currentJobIndication = JobIndication;
                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: Random | Job Difficulty: {JobDifficulty} | Job Range: {startRange}-{endRange}", ConsoleColor.Blue).ConfigureAwait(false);

                while (currentJobIndication == JobIndication)
                {
                    if (!PoolService.IsLoggedIn || PoolService.CancellationTokenSource.IsCancellationRequested)
                        break;

                    var firstNumber = decimal.Parse(MiningUtility.GenerateNumberMathCalculation(startRange, endRange));
                    var secondNumber = decimal.Parse(MiningUtility.GenerateNumberMathCalculation(startRange, endRange));

                    foreach (var randomOperator in MiningUtility.RandomOperatorCalculation)
                    {
                        if (!PoolService.IsLoggedIn || PoolService.CancellationTokenSource.IsCancellationRequested)
                            break;

                        if (currentJobIndication != JobIndication)
                            break;

                        await DoCalculationAsync(firstNumber, secondNumber, randomOperator, "Random", threadIndex).ConfigureAwait(false);
                        await DoCalculationAsync(secondNumber, firstNumber, randomOperator, "Random", threadIndex).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task DoCalculationAsync(decimal firstNumber, decimal secondNumber, string operatorSymbol, string jobType, int threadIndex)
        {
            if (firstNumber < 2 || firstNumber > BlockDifficulty)
                return;

            if (secondNumber < 2 || secondNumber > BlockDifficulty)
                return;

            var calculation = $"{firstNumber} {operatorSymbol} {secondNumber}";

            if (SubmittedShares.ContainsKey(calculation))
                return;

            decimal result;

            switch (operatorSymbol)
            {
                case "+":
                    result = firstNumber + secondNumber;
                    break;

                case "-":
                    result = firstNumber - secondNumber;
                    break;

                case "*":
                    result = firstNumber * secondNumber;
                    break;

                case "/":
                    result = firstNumber / secondNumber;
                    break;

                case "%":
                    result = firstNumber % secondNumber;
                    break;

                default:
                    throw new ArgumentException("Invalid operator used.");
            }

            if (result - Math.Round(result, 0) != 0)
                return;

            if (result < 2 || result > BlockDifficulty)
                return;

            var encryptedShare = MakeEncryptedShare(calculation, threadIndex);

            if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                return;

            var hashEncryptedShare = MiningUtility.GenerateSHA512(encryptedShare);

            var hashEncryptedKeyShare = MiningUtility.EncryptXorShare(hashEncryptedShare, JobEncryptionKey);
            hashEncryptedKeyShare = MiningUtility.HashJobToHexString(hashEncryptedKeyShare);

            if (!JobIndication.Contains(hashEncryptedKeyShare) && hashEncryptedShare != BlockIndication)
                return;

            if (SubmittedShares.ContainsKey(calculation))
                return;

            if (!SubmittedShares.TryAdd(calculation, true))
                return;

            if (hashEncryptedShare == BlockIndication)
                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: {jobType} | Block found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green).ConfigureAwait(false);
            else if (JobIndication.Contains(hashEncryptedShare))
                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: {jobType} | Job found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green).ConfigureAwait(false);

            var share = new JObject
            {
                { PoolPacket.Type, PoolPacketType.Submit },
                { PoolSubmitPacket.SubmitResult, result },
                { PoolSubmitPacket.SubmitFirstNumber, firstNumber },
                { PoolSubmitPacket.SubmitSecondNumber, secondNumber },
                { PoolSubmitPacket.SubmitOperator, operatorSymbol },
                { PoolSubmitPacket.SubmitShare, encryptedShare },
                { PoolSubmitPacket.SubmitHash, hashEncryptedKeyShare }
            };

            _ = Task.Run(() => PoolService.SendPacketToPoolNetworkAsync(share.ToString(Formatting.None)));
        }

        private string MakeEncryptedShare(string calculation, int threadIndex)
        {
            try
            {
                var encryptedShare = MiningUtility.StringToHexString(calculation + BlockTimestampCreate);

                // Static XOR Encryption -> Key updated from the current mining method.
                encryptedShare = MiningUtility.EncryptXorShare(encryptedShare, JobXorKey.ToString());

                // Dynamic AES Encryption -> Size and Key's from the current mining method and the current block key encryption.
                encryptedShare = MiningUtility.EncryptAesShareRound(encryptedShare, JobAesKeyBytes, JobAesIvBytes, JobAesSize, JobAesRound);

                // Static XOR Encryption -> Key from the current mining method
                encryptedShare = MiningUtility.EncryptXorShare(encryptedShare, JobXorKey.ToString());

                // Static AES Encryption -> Size and Key's from the current mining method.
                encryptedShare = MiningUtility.EncryptAesShare(encryptedShare, JobAesKeyBytes, JobAesIvBytes, JobAesSize);

                // Generate SHA512 HASH for the share.
                encryptedShare = MiningUtility.GenerateSHA512(encryptedShare);

                //HashesCalculated[threadIndex]++;

                return encryptedShare;
            }
            catch
            {
                return ClassAlgoErrorEnumeration.AlgoError;
            }
        }

        public void Dispose()
        {

        }
    }
}