using System;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using TheDialgaTeam.Xiropht.Xirorig.Mining;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    [Config(typeof(Config))]
    public class MakeEncryptedShareBenchmark
    {
        private string TestData { get; }

        private RijndaelManaged Aes { get; } = new RijndaelManaged();

        private ICryptoTransform JobAesCryptoTransform { get; }

        public MakeEncryptedShareBenchmark()
        {
            TestData = "100000000 + 100000000" + DateTimeOffset.Now.ToUnixTimeSeconds();

            using (var pdb = new PasswordDeriveBytes("128", Encoding.UTF8.GetBytes("128")))
            {
                var jobAesKeyBytes = pdb.GetBytes(128 / 8);
                var jobAesIvBytes = pdb.GetBytes(128 / 8);

                Aes.BlockSize = 128;
                Aes.KeySize = 128;
                Aes.Key = jobAesKeyBytes;
                Aes.IV = jobAesIvBytes;

                JobAesCryptoTransform = Aes.CreateEncryptor();
            }
        }

        [Benchmark]
        public string MakeEncryptedShare()
        {
            var encryptedShare = MiningUtility.ConvertStringToHexAndEncryptXorShare(TestData, "128");
            encryptedShare = MiningUtility.EncryptAesShareRoundAndEncryptXorShare(JobAesCryptoTransform, encryptedShare, 1, "128");
            encryptedShare = MiningUtility.EncryptAesShare(JobAesCryptoTransform, encryptedShare);
            encryptedShare = MiningUtility.GenerateSha512(encryptedShare);

            return encryptedShare;
        }
    }
}