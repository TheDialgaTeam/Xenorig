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
        public enum JobType
        {
            AdditionJob = 1,
            SubtractionJob = 2,
            MultiplicationJob = 3,
            DivisionJob = 4,
            ModulusJob = 5,
            RandomJob = 0
        }

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

        public int JobMethodAesRound { get; private set; }

        public int JobMethodAesSize { get; private set; }

        public string JobMethodAesKey { get; private set; }

        public int JobMethodXorKey { get; private set; }

        public int TotalShareToFind => JobIndication.Length / BlockIndication.Length;

        public long TotalHashCalculated { get; private set; }

        public ConcurrentDictionary<int, long> HashesCalculated { get; } = new ConcurrentDictionary<int, long>();

        public ConcurrentDictionary<string, bool> SubmittedShares { get; } = new ConcurrentDictionary<string, bool>();

        private LoggerService LoggerService { get; }

        private ConfigService ConfigService { get; }

        private PoolService PoolService { get; }

        private Task StartMiningTask { get; set; }

        private List<Thread> RandomJobThreads { get; } = new List<Thread>();

        private byte[] AesKeyBytes { get; set; }

        private byte[] AesIvBytes { get; set; }

        public PoolMiner(LoggerService loggerService, ConfigService configService, PoolService poolService)
        {
            LoggerService = loggerService;
            ConfigService = configService;
            PoolService = poolService;
        }

        private static void SetThreadPriority(Thread thread, int threadPriority)
        {
            switch (threadPriority)
            {
                case 0:
                    thread.Priority = ThreadPriority.Lowest;
                    break;

                case 1:
                    thread.Priority = ThreadPriority.BelowNormal;
                    break;

                case 2:
                    thread.Priority = ThreadPriority.Normal;
                    break;

                case 3:
                    thread.Priority = ThreadPriority.AboveNormal;
                    break;

                case 4:
                    thread.Priority = ThreadPriority.Highest;
                    break;

                default:
                    thread.Priority = ThreadPriority.Normal;
                    break;
            }
        }

        public void UpdateJob(string packet)
        {
            var json = JObject.Parse(packet);

            BlockId = int.Parse(json[PoolJobPacket.Block].ToString());
            BlockTimestampCreate = json[PoolJobPacket.BlockTimestampCreate].ToString();
            BlockKey = json[PoolJobPacket.BlockKey].ToString();
            BlockIndication = json[PoolJobPacket.BlockIndication].ToString();
            BlockDifficulty = decimal.Parse(json[PoolJobPacket.BlockDifficulty].ToString());
            JobIndication = ClassUtils.DecompressData(json[PoolJobPacket.JobIndication].ToString());
            JobDifficulty = decimal.Parse(json[PoolJobPacket.JobDifficulty].ToString());
            JobMinRange = decimal.Parse(json[PoolJobPacket.JobMinRange].ToString());
            JobMaxRange = decimal.Parse(json[PoolJobPacket.JobMaxRange].ToString());
            JobMethodName = json[PoolJobPacket.JobMethodName].ToString();
            JobMethodAesRound = int.Parse(json[PoolJobPacket.JobMethodAesRound].ToString());
            JobMethodAesSize = int.Parse(json[PoolJobPacket.JobMethodAesSize].ToString());
            JobMethodAesKey = json[PoolJobPacket.JobMethodAesKey].ToString();
            JobMethodXorKey = int.Parse(json[PoolJobPacket.JobMethodXorKey].ToString());

            using (var pdb = new PasswordDeriveBytes(BlockKey, Encoding.UTF8.GetBytes(JobMethodAesKey)))
            {
                AesKeyBytes = pdb.GetBytes(JobMethodAesSize / 8);
                AesIvBytes = pdb.GetBytes(JobMethodAesSize / 8);
            }

            SubmittedShares.Clear();
        }

        public void StartMining()
        {
            StartMiningTask = Task.Factory.StartNew(async () =>
            {
                do
                {
                    while (JobIndication == null)
                    {
                        if (!PoolService.IsLoggedIn || PoolService.CancellationTokenSource.IsCancellationRequested)
                            break;

                        await Task.Delay(1000).ConfigureAwait(false);
                    }

                    if (RandomJobThreads.Count == 0)
                    {
                        for (var i = 0; i < ConfigService.RandomJobThreads.Length; i++)
                        {
                            HashesCalculated[i] = 0;

                            var threadIndex = i;
                            var miningThread = ConfigService.RandomJobThreads[i];

                            if (miningThread.ThreadAffinityToCpu > 0)
                            {
#if WIN
                                var thread = new DistributedThread(async () => await DoRandomJob(threadIndex, miningThread.PrioritizePoolSharesVsBlock).ConfigureAwait(false)) { ProcessorAffinity = 1 << miningThread.ThreadAffinityToCpu };
                                thread.ManagedThread.IsBackground = true;
                                SetThreadPriority(thread.ManagedThread, miningThread.ThreadPriority);
                                thread.Start();
                                RandomJobThreads.Add(thread.ManagedThread);
#else
                                var thread = new Thread(async () => await DoRandomJob(threadIndex, miningThread.PrioritizePoolSharesVsBlock).ConfigureAwait(false)) { IsBackground = true };
                                SetThreadPriority(thread, miningThread.ThreadPriority);
                                thread.Start();
                                RandomJobThreads.Add(thread);
#endif
                            }
                            else
                            {
                                var thread = new Thread(async () => await DoRandomJob(threadIndex, miningThread.PrioritizePoolSharesVsBlock).ConfigureAwait(false)) { IsBackground = true };
                                SetThreadPriority(thread, miningThread.ThreadPriority);
                                thread.Start();
                                RandomJobThreads.Add(thread);
                            }
                        }
                    }

                    TotalHashCalculated = 0;

                    for (var i = 0; i < HashesCalculated.Count; i++)
                    {
                        TotalHashCalculated += HashesCalculated[i];
                        HashesCalculated[i] = 0;
                    }

                    await Task.Delay(1000).ConfigureAwait(false);
                } while (PoolService.IsLoggedIn && !PoolService.CancellationTokenSource.IsCancellationRequested);

                RandomJobThreads.Clear();
            }, PoolService.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();
        }

        private async Task DoRandomJob(int threadIndex, bool prioritizePoolSharesVsBlock)
        {
            var poolSharesDone = false;
            var currentJobIndication = JobIndication;

            while (PoolService.IsLoggedIn && !PoolService.CancellationTokenSource.IsCancellationRequested)
            {
                if (currentJobIndication != JobIndication)
                {
                    poolSharesDone = false;
                    currentJobIndication = JobIndication;
                }

                decimal startRange, endRange;

                if (prioritizePoolSharesVsBlock && !poolSharesDone)
                    (startRange, endRange) = MiningUtility.GetJobRange(JobMaxRange - JobMinRange + 1, ConfigService.RandomJobThreads.Length, threadIndex, JobMinRange);
                else
                    (startRange, endRange) = MiningUtility.GetJobRange(BlockDifficulty - 1, ConfigService.RandomJobThreads.Length, threadIndex, 2);

                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: Random | Job Difficulty: {JobDifficulty} | Job Range: {startRange}-{endRange}", ConsoleColor.Blue).ConfigureAwait(false);

                while (currentJobIndication == JobIndication)
                {
                    if (!PoolService.IsLoggedIn || PoolService.CancellationTokenSource.IsCancellationRequested)
                        break;

                    if (prioritizePoolSharesVsBlock && SubmittedShares.Count >= TotalShareToFind && !poolSharesDone)
                    {
                        poolSharesDone = true;
                        break;
                    }

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

            if (!JobIndication.Contains(hashEncryptedShare) && hashEncryptedShare != BlockIndication)
                return;

            if (!SubmittedShares.TryAdd(calculation, true))
                return;

            if (JobIndication.Contains(hashEncryptedShare))
                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: {jobType} | Job found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green).ConfigureAwait(false);

            if (hashEncryptedShare == BlockIndication)
                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: {jobType} | Block found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green).ConfigureAwait(false);

            var share = new JObject
            {
                { PoolPacket.Type, PoolPacketType.Submit },
                { PoolSubmitPacket.SubmitResult, result },
                { PoolSubmitPacket.SubmitFirstNumber, firstNumber },
                { PoolSubmitPacket.SubmitSecondNumber, secondNumber },
                { PoolSubmitPacket.SubmitOperator, operatorSymbol },
                { PoolSubmitPacket.SubmitShare, encryptedShare },
                { PoolSubmitPacket.SubmitHash, hashEncryptedShare }
            };

            await PoolService.SendPacketToPoolNetworkAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
        }

        private string MakeEncryptedShare(string calculation, int threadIndex)
        {
            try
            {
                var encryptedShare = MiningUtility.StringToHexString(calculation + BlockTimestampCreate);

                // Static XOR Encryption -> Key updated from the current mining method.
                encryptedShare = MiningUtility.EncryptXorShare(encryptedShare, JobMethodXorKey.ToString());

                // Dynamic AES Encryption -> Size and Key's from the current mining method and the current block key encryption.
                encryptedShare = MiningUtility.EncryptAesShareRound(encryptedShare, AesKeyBytes, AesIvBytes, JobMethodAesSize, JobMethodAesRound);

                // Static XOR Encryption -> Key from the current mining method
                encryptedShare = MiningUtility.EncryptXorShare(encryptedShare, JobMethodXorKey.ToString());

                // Static AES Encryption -> Size and Key's from the current mining method.
                encryptedShare = MiningUtility.EncryptAesShare(encryptedShare, AesKeyBytes, AesIvBytes, JobMethodAesSize);

                // Generate SHA512 HASH for the share.
                encryptedShare = MiningUtility.GenerateSHA512(encryptedShare);

                HashesCalculated[threadIndex]++;

                return encryptedShare;
            }
            catch
            {
                return ClassAlgoErrorEnumeration.AlgoError;
            }
        }

        public void Dispose()
        {
            StartMiningTask?.Dispose();
        }
    }
}