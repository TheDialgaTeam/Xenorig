using System;
using BenchmarkDotNet.Attributes;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    [Config(typeof(Config))]
    public class EncryptXorShareBenchmark
    {
        private string TestData { get; }

        public EncryptXorShareBenchmark()
        {
            TestData = "50000000 + 50000000" + DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        private static unsafe string EncryptXorShare(string value, string key)
        {
            var valueLength = value.Length;
            var keyLength = key.Length;
            var result = new string('\0', valueLength);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;
                var keyIndex = 0;

                for (var i = 0; i < valueLength; i++)
                {
                    *charPtr = (char) (value[i] ^ key[keyIndex]);
                    charPtr++;
                    keyIndex++;

                    if (keyIndex >= keyLength)
                        keyIndex = 0;
                }
            }

            return result;
        }

        [Benchmark]
        public string EncryptXorShare()
        {
            return EncryptXorShare(TestData, "128");
        }
    }
}