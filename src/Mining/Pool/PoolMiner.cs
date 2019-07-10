using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Mining.Pool.Packet;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Mining;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;
using TheDialgaTeam.Xiropht.Xirorig.Setting;
using Xiropht_Connector_All.Utils;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining.Pool
{
    public sealed class PoolMiner : AbstractMiner
    {
        private string JobEncryptionKey { get; set; }

        private ConcurrentDictionary<string, string> SharesSubmitted { get; } = new ConcurrentDictionary<string, string>();

        private int SharesToFind { get; set; }

        public PoolMiner(Program program, LoggerService loggerService, ConfigService configService, MiningService miningService) : base(program, loggerService, configService, miningService)
        {
        }

        public override void UpdateJob(string packet)
        {
            var json = JObject.Parse(packet);

            BlockId = decimal.Parse(json[PoolJobPacket.Block].ToString());
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
            JobXorKey = json[PoolJobPacket.JobMethodXorKey].ToString();
            JobEncryptionKey = json[PoolJobPacket.JobKeyEncryption].ToString();

            using (var pdb = new PasswordDeriveBytes(BlockKey, Encoding.UTF8.GetBytes(JobAesKey)))
            {
                var aes = new RijndaelManaged { BlockSize = JobAesSize, KeySize = JobAesSize, Key = pdb.GetBytes(JobAesSize / 8), IV = pdb.GetBytes(JobAesSize / 8) };
                CryptoTransform = aes.CreateEncryptor();
            }

            SharesToFind = JobIndication.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Length - 1;

            SharesSubmitted.Clear();
        }

        protected override async Task DoRandomJobAsync(int threadIndex, Config.MiningThread miningThread)
        {
            var listener = MiningService.Listener;
            var randomJobThreads = ConfigService.RandomJobThreads;
            
            var miningPrioritySharesThreads = randomJobThreads.Where(a => a.JobType == Config.MiningJob.RandomJob && a.ShareRange && a.MiningPriority == Config.MiningPriority.Shares).ToArray();
            var miningPrioritySharesThreadsLength = miningPrioritySharesThreads.Length;
            
            var miningPriorityBlockThreads = randomJobThreads.Where(a => a.JobType == Config.MiningJob.RandomJob && a.ShareRange && a.MiningPriority != Config.MiningPriority.Shares).ToArray();
            var miningPriorityBlockThreadsLength = miningPriorityBlockThreads.Length;

            var loggerService = LoggerService;

            const string jobType = "Random";

            while (listener.ConnectionStatus == ConnectionStatus.Connected && listener.IsLoggedIn)
            {
                if (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                    break;

                while (JobIndication == null)
                {
                    if (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                        break;

                    await Task.Delay(1000).ConfigureAwait(false);
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

                await loggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: Random | Job Difficulty: {JobDifficulty} | Job Range: {startRange}-{endRange}", ConsoleColor.Blue).ConfigureAwait(false);

                var currentJobIndication = JobIndication;
                var currentBlockIndication = BlockIndication;

                var sharesSubmitted = SharesSubmitted;
                var sharesToFind = SharesToFind;

                while (currentJobIndication == JobIndication)
                {
                    if (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                        break;

                    if (currentJobIndication != JobIndication)
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
            var sharesSubmitted = SharesSubmitted;
            var jobIndication = JobIndication;
            var blockIndication = BlockIndication;

            if (sharesSubmitted.Values.Contains(blockIndication))
                return;

            if (sharesSubmitted.Count >= SharesToFind)
                return;

            var calculation = $"{firstNumber} {operatorSymbol} {secondNumber}";

            if (sharesSubmitted.ContainsKey(calculation))
                return;

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

            var encryptedShare = MakeEncryptedShare(calculation, threadIndex);

            if (encryptedShare == ClassAlgoErrorEnumeration.AlgoError)
                return;

            var hashEncryptedShare = MiningUtility.ComputeHash(HashAlgorithm, encryptedShare);

            var hashEncryptedKeyShare = MiningUtility.EncryptXorShare(hashEncryptedShare, JobEncryptionKey);
            hashEncryptedKeyShare = MiningUtility.HashJobToHexString(hashEncryptedKeyShare);

            if (!jobIndication.Contains(hashEncryptedKeyShare) && hashEncryptedShare != blockIndication)
                return;

            if (sharesSubmitted.ContainsKey(calculation))
                return;

            if (!sharesSubmitted.TryAdd(calculation, hashEncryptedShare))
                return;

            if (hashEncryptedShare == blockIndication)
                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: {jobType} | Block found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green).ConfigureAwait(false);
            else if (jobIndication.Contains(hashEncryptedKeyShare))
                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: {jobType} | Job found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green).ConfigureAwait(false);

            var share = new JObject
            {
                { PoolPacket.Type, PoolPacketType.Submit },
                { PoolSubmitPacket.SubmitResult, result },
                { PoolSubmitPacket.SubmitFirstNumber, firstNumber },
                { PoolSubmitPacket.SubmitSecondNumber, secondNumber },
                { PoolSubmitPacket.SubmitOperator, operatorSymbol.ToString() },
                { PoolSubmitPacket.SubmitShare, encryptedShare },
                { PoolSubmitPacket.SubmitHash, hashEncryptedKeyShare }
            };

            await MiningService.Listener.SendPacketToPoolNetworkAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
        }
    }
}