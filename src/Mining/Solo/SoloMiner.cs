using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Mining;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;
using TheDialgaTeam.Xiropht.Xirorig.Setting;
using Xiropht_Connector_All.SoloMining;
using Xiropht_Connector_All.Utils;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining.Solo
{
    public sealed class SoloMiner : AbstractMiner
    {
        private ConcurrentDictionary<string, string> SharesSubmitted { get; } = new ConcurrentDictionary<string, string>();

        public SoloMiner(Program program, LoggerService loggerService, ConfigService configService, MiningService miningService) : base(program, loggerService, configService, miningService)
        {
        }

        public override void UpdateJob(string packet)
        {
            var json = JObject.Parse(packet);

            BlockId = decimal.Parse(json["ID"].ToString());
            BlockTimestampCreate = json["TIMESTAMP"].ToString();
            BlockKey = json["KEY"].ToString();
            BlockIndication = json["INDICATION"].ToString();
            BlockDifficulty = decimal.Parse(json["DIFFICULTY"].ToString());
            JobIndication = BlockIndication;
            JobDifficulty = BlockDifficulty;

            var job = json["JOB"].ToString().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            JobMinRange = decimal.Parse(job[0]);
            JobMaxRange = decimal.Parse(job[1]);
            JobMethodName = json["METHOD"].ToString();
            JobAesRound = int.Parse(json["AESROUND"].ToString());
            JobAesSize = int.Parse(json["AESSIZE"].ToString());
            JobAesKey = json["AESKEY"].ToString();
            JobXorKey = json["XORKEY"].ToString();

            using (var pdb = new PasswordDeriveBytes(BlockKey, Encoding.UTF8.GetBytes(JobAesKey)))
            {
                var aes = new RijndaelManaged { BlockSize = JobAesSize, KeySize = JobAesSize, Key = pdb.GetBytes(JobAesSize / 8), IV = pdb.GetBytes(JobAesSize / 8) };
                CryptoTransform = aes.CreateEncryptor();
            }

            SharesSubmitted.Clear();
        }

        protected override async Task DoRandomJobAsync(int threadIndex, Config.MiningThread miningThread)
        {
            var listener = MiningService.Listener;
            var randomJobThreads = ConfigService.RandomJobThreads;

            var miningPriorityThreads = randomJobThreads.Where(a => a.JobType == Config.MiningJob.RandomJob && a.ShareRange).ToArray();
            var miningPriorityThreadsLength = miningPriorityThreads.Length;

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
                    (startRange, endRange) = MiningUtility.GetJobRangeByPercentage(jobMinRange, jobMaxRange, miningThread.MinMiningRangePercentage, miningThread.MaxMiningRangePercentage);
                else
                {
                    var totalPossibilities = jobMaxRange - jobMinRange + 1;
                    var index = 0;

                    for (var i = 0; i < miningPriorityThreadsLength; i++)
                    {
                        if (miningPriorityThreads[i] != miningThread)
                            continue;

                        index = i;
                    }

                    (startRange, endRange) = MiningUtility.GetJobRange(totalPossibilities, miningPriorityThreadsLength, index, jobMinRange);
                }

                await loggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: Random | Job Difficulty: {JobDifficulty} | Job Range: {startRange}-{endRange}", ConsoleColor.Blue).ConfigureAwait(false);

                var currentJobIndication = JobIndication;
                var currentBlockIndication = BlockIndication;

                var sharesSubmitted = SharesSubmitted;

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

                    if (firstNumber < jobMinRange || firstNumber > jobMaxRange)
                        continue;

                    if (secondNumber < jobMinRange || secondNumber > jobMaxRange)
                        continue;

                    foreach (var randomOperator in MiningUtility.RandomOperatorCalculation)
                    {
                        if (listener.ConnectionStatus != ConnectionStatus.Connected || !listener.IsLoggedIn)
                            break;

                        if (currentJobIndication != JobIndication)
                            break;

                        if (sharesSubmitted.Values.Contains(currentBlockIndication))
                            break;

                        await DoCalculationAsync(firstNumber, secondNumber, randomOperator, jobType, threadIndex).ConfigureAwait(false);
                        await DoCalculationAsync(secondNumber, firstNumber, randomOperator, jobType, threadIndex).ConfigureAwait(false);
                    }

                    if (sharesSubmitted.Values.Contains(currentBlockIndication))
                        break;
                }

                if (sharesSubmitted.Values.Contains(currentBlockIndication))
                    await WaitForNextBlockAsync(currentBlockIndication).ConfigureAwait(false);
            }
        }

        private async Task DoCalculationAsync(decimal firstNumber, decimal secondNumber, char operatorSymbol, string jobType, int threadIndex)
        {
            var sharesSubmitted = SharesSubmitted;
            var blockIndication = BlockIndication;

            if (sharesSubmitted.Values.Contains(blockIndication))
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

            if (hashEncryptedShare != blockIndication)
                return;

            if (sharesSubmitted.ContainsKey(calculation))
                return;

            if (!sharesSubmitted.TryAdd(calculation, hashEncryptedShare))
                return;

            if (hashEncryptedShare == blockIndication)
                await LoggerService.LogMessageAsync($"Thread: {threadIndex + 1} | Job Type: {jobType} | Block found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green).ConfigureAwait(false);

            var share = new JObject
            {
                { "packet", $"{ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob}|{encryptedShare}|{result:F0}|{calculation}|{hashEncryptedShare}|{BlockId:F0}|Xirorig/{Assembly.GetExecutingAssembly().GetName().Version}" },
                { "isEncrypted", true }
            };

            await MiningService.Listener.SendPacketToNetworkAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
        }
    }
}