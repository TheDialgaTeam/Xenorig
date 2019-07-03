using System;
using BenchmarkDotNet.Attributes;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    public class EncryptXorShareBenchmark
    {
        private string TestData { get; }

        public EncryptXorShareBenchmark()
        {
            TestData = "50000000 + 50000000" + DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        private static unsafe string EncryptXorShare_Mono(string value, string key)
        {
            var valueLength = value.Length;
            var keyLength = key.Length;
            var result = new string('\0', valueLength);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;

                for (var i = 0; i < valueLength; i++)
                {
                    *charPtr = (char) (value[i] ^ key[i % keyLength]);
                    charPtr++;
                }
            }

            return result;
        }

        private static string EncryptXorShare_NetCore(string value, string key)
        {
            return string.Create(value.Length, (value, key), (result, state) =>
            {
                for (var i = 0; i < state.value.Length; i++)
                    result[i] = (char) (state.value[i] ^ state.key[i % state.key.Length]);
            });
        }

        [Benchmark]
        public string EncryptXorShare_Mono()
        {
            return EncryptXorShare_Mono(TestData, "128");
        }

        [Benchmark]
        public string EncryptXorShare_NetCore()
        {
            return EncryptXorShare_NetCore(TestData, "128");
        }
    }
}