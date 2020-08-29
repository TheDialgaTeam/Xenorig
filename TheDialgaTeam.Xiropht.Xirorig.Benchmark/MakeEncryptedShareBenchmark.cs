using System;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;

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
                Aes.BlockSize = 128;
                Aes.KeySize = 128;
                Aes.Key = pdb.GetBytes(128 / 8);
                Aes.IV = pdb.GetBytes(128 / 8);

                JobAesCryptoTransform = Aes.CreateEncryptor();
            }
        }

        [Benchmark]
        public string MakeEncryptedShare()
        {
            //var encryptedShare = MiningUtility.ConvertStringToHexAndEncryptXorShare(TestData, "128");
            //encryptedShare = MiningUtility.EncryptAesShareAndEncryptXorShare(JobAesCryptoTransform, encryptedShare, 1, "128");
            //encryptedShare = MiningUtility.ComputeHash(SHA512.Create(), encryptedShare);

            //return encryptedShare;
            return string.Empty;
        }
    }
}