using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Mining;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;
using Xiropht_Connector_All.SoloMining;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining.Solo
{
    public sealed class SoloMiner : AbstractMiner
    {
        private string UserAgent { get; }

        public SoloMiner(LoggerService loggerService, ConfigService configService, MiningService miningService) : base(loggerService, configService, miningService)
        {
            UserAgent = $"Xirorig/{Assembly.GetExecutingAssembly().GetName().Version}";
        }

        public override void UpdateJob(string packet)
        {
            var json = JObject.Parse(packet);

            BlockId = long.Parse(json["ID"].ToString());
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

            SharesToFind = 1;
            SharesSubmitted.Clear();
        }

        protected override async Task ValidateAndSubmitAsync(decimal firstNumber, decimal secondNumber, char operatorSymbol, string jobType, int threadIndex, decimal result, string encryptedShare, string hashEncryptedShare)
        {
            var blockIndication = BlockIndication;

            if (hashEncryptedShare != blockIndication)
                return;

            if (!SharesSubmitted.TryAdd($"{firstNumber} {operatorSymbol} {secondNumber}", hashEncryptedShare))
                return;

            if (hashEncryptedShare == blockIndication)
                LoggerService.LogMessage($"Thread: {threadIndex + 1} | Job Type: {jobType} | Block found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green);

            var share = new JObject
            {
                { "packet", $"{ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob}|{encryptedShare}|{result}|{firstNumber} {operatorSymbol.ToString()} {secondNumber}|{hashEncryptedShare}|{BlockId}|{UserAgent}" },
                { "isEncrypted", true }
            };

            await MiningService.Listener.SendPacketToNetworkAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
        }
    }
}