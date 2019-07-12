using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Mining.Pool.Packet;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Mining;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining.Pool
{
    public sealed class PoolMiner : AbstractMiner
    {
        private string JobEncryptionKey { get; set; }

        public PoolMiner(LoggerService loggerService, ConfigService configService, MiningService miningService) : base(loggerService, configService, miningService)
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

        protected override async Task ValidateAndSubmitAsync(decimal firstNumber, decimal secondNumber, char operatorSymbol, string jobType, int threadIndex, decimal result, string encryptedShare, string hashEncryptedShare)
        {
            var jobIndication = JobIndication;
            var blockIndication = BlockIndication;

            var hashEncryptedKeyShare = MiningUtility.EncryptXorShare(hashEncryptedShare, JobEncryptionKey);
            hashEncryptedKeyShare = MiningUtility.HashJobToHexString(hashEncryptedKeyShare);

            if (!jobIndication.Contains(hashEncryptedKeyShare) && hashEncryptedShare != blockIndication)
                return;

            if (!SharesSubmitted.TryAdd($"{firstNumber} {operatorSymbol} {secondNumber}", hashEncryptedShare))
                return;

            if (hashEncryptedShare == blockIndication)
                LoggerService.LogMessage($"Thread: {threadIndex + 1} | Job Type: {jobType} | Block found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green);
            else if (jobIndication.Contains(hashEncryptedKeyShare))
                LoggerService.LogMessage($"Thread: {threadIndex + 1} | Job Type: {jobType} | Job found: {firstNumber} {operatorSymbol} {secondNumber} = {result}", ConsoleColor.Green);

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

            await MiningService.Listener.SendPacketToNetworkAsync(share.ToString(Formatting.None)).ConfigureAwait(false);
        }
    }
}