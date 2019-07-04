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

        private static unsafe string ConvertStringToHexAndEncryptXorShare(string value, string key)
        {
            var base16CharRepresentation = Base16CharRepresentation;
            var valueLength = value.Length;
            var keyLength = key.Length;
            var result = new string('\0', valueLength * 2);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;
                var keyIndex = 0;

                for (var i = 0; i < valueLength; i++)
                {
                    *charPtr = (char) (base16CharRepresentation[value[i] >> 4] ^ key[keyIndex]);
                    charPtr++;
                    keyIndex++;

                    if (keyIndex >= keyLength)
                        keyIndex = 0;

                    *charPtr = (char) (base16CharRepresentation[value[i] & 15] ^ key[keyIndex]);
                    charPtr++;
                    keyIndex++;

                    if (keyIndex >= keyLength)
                        keyIndex = 0;
                }
            }

            return result;
        }

        [Benchmark]
        public string ConvertStringToHexAndEncryptXorShare()
        {
            return ConvertStringToHexAndEncryptXorShare(TestData, "128");
        }
    }
}