using System;
using BenchmarkDotNet.Attributes;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    public class EncryptStringWithStringHexAndXorBenchmark
    {
        private static char[] HexStringRepresentation { get; } = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private string TestData { get; }

        public EncryptStringWithStringHexAndXorBenchmark()
        {
            TestData = "500 + 500" + DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        private static string EncryptStringWithStringHexAndXor_NetCore(string value, string key)
        {
            return string.Create(value.Length * 2, (value, key), (result, state) =>
            {
                var valueLength = state.value.Length;
                var keyLength = state.key.Length;

                for (var i = 0; i < valueLength; i++)
                {
                    result[i * 2] = (char) (HexStringRepresentation[state.value[i] >> 4] ^ state.key[i * 2 % keyLength]);
                    result[i * 2 + 1] = (char) (HexStringRepresentation[state.value[i] & 15] ^ state.key[(i * 2 + 1) % keyLength]);
                }
            });
        }

        private static string EncryptStringWithStringHexAndXor_Mono(string value, string key)
        {
            var valueLength = value.Length;
            var keyLength = key.Length;
            var result = new char[valueLength * 2];

            for (var i = 0; i < valueLength; i++)
            {
                result[i * 2] = (char) (HexStringRepresentation[value[i] >> 4] ^ key[i * 2 % keyLength]);
                result[i * 2 + 1] = (char) (HexStringRepresentation[value[i] & 15] ^ key[(i * 2 + 1) % keyLength]);
            }

            return new string(result);
        }

        [Benchmark]
        public string EncryptStringWithStringHexAndXor_NetCore()
        {
            return EncryptStringWithStringHexAndXor_NetCore(TestData, "128");
        }

        [Benchmark]
        public string EncryptStringWithStringHexAndXor_Mono()
        {
            return EncryptStringWithStringHexAndXor_Mono(TestData, "128");
        }
    }
}