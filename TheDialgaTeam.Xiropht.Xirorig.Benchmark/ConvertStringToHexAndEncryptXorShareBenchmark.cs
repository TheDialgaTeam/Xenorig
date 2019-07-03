using System;
using BenchmarkDotNet.Attributes;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    [Config(typeof(Config))]
    public class ConvertStringToHexAndEncryptXorShareBenchmark
    {
        private static char[] Base16CharRepresentation { get; } = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private string TestData { get; }

        public ConvertStringToHexAndEncryptXorShareBenchmark()
        {
            TestData = "50000000 + 50000000" + DateTimeOffset.Now.ToUnixTimeSeconds();
        }

#if NETCOREAPP
        private static string ConvertStringToHexAndEncryptXorShare(string value, string key)
#else
        private static unsafe string ConvertStringToHexAndEncryptXorShare(string value, string key)
#endif
        {
#if NETCOREAPP
            return string.Create(value.Length * 2, (value, key), (result, state) =>
            {
                var base16CharRepresentation = Base16CharRepresentation;

                for (var i = 0; i < state.value.Length; i++)
                {
                    result[i * 2] = (char) (base16CharRepresentation[state.value[i] >> 4] ^ key[i * 2 % state.key.Length]);
                    result[i * 2 + 1] = (char) (base16CharRepresentation[state.value[i] & 15] ^ key[i * 2 % state.key.Length]);
                }
            });
#else
            var base16CharRepresentation = Base16CharRepresentation;
            var valueLength = value.Length;
            var keyLength = key.Length;
            var result = new string('\0', valueLength * 2);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;

                for (var i = 0; i < valueLength; i++)
                {
                    *charPtr = (char) (base16CharRepresentation[value[i] >> 4] ^ key[i * 2 % keyLength]);
                    charPtr++;
                    *charPtr = (char) (base16CharRepresentation[value[i] & 15] ^ key[(i * 2 + 1) % keyLength]);
                    charPtr++;
                }
            }

            return result;
#endif
        }

        [Benchmark]
        public string ConvertStringToHexAndEncryptXorShare()
        {
            return ConvertStringToHexAndEncryptXorShare(TestData, "128");
        }
    }
}