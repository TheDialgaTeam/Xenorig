using System;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using TheDialgaTeam.Xiropht.Xirorig.Miner;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    [Config(typeof(Config))]
    public class MakeEncryptedShareBenchmark
    {
        private string TestData { get; }

        private RijndaelManaged Aes { get; } = new RijndaelManaged();

        private SHA512 Sha512 { get; } = SHA512.Create();

        private ICryptoTransform JobAesCryptoTransform { get; }

        private string key { get; } = "128";

        private byte[] keyBytes { get; }

        public MakeEncryptedShareBenchmark()
        {
            TestData = "100000000 + 100000000" + DateTimeOffset.Now.ToUnixTimeSeconds();

            using var pdb = new PasswordDeriveBytes("128", Encoding.UTF8.GetBytes("128"));

            Aes.BlockSize = 128;
            Aes.KeySize = 128;
            Aes.Key = pdb.GetBytes(128 / 8);
            Aes.IV = pdb.GetBytes(128 / 8);

            JobAesCryptoTransform = Aes.CreateEncryptor();
            keyBytes = Encoding.UTF8.GetBytes(key);
        }

        [Benchmark]
        public string MakeEncryptedShare()
        {
            var encryptedShare = MiningUtility.MakeEncryptedShare(TestData, key, 1, JobAesCryptoTransform, Sha512);

            return encryptedShare;
        }
    }
}