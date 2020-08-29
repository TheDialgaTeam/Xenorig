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
            TestData = "100000000 + 100000000" + DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        private static unsafe string ConvertStringToHexAndEncryptXorShare(string value, string key)
        {
            var valueLength = value.Length;
            var result = new string('\0', valueLength * 2);

            fixed (char* valuePtr = value, keyPtr = key, base16CharRepresentationPtr = Base16CharRepresentation, resultPtr = result)
            {
                var valueCharPtr = valuePtr;
                var resultCharPtr = resultPtr;

                var keyLength = key.Length;
                var keyIndex = 0;

                for (var i = 0; i < valueLength; i++)
                {
                    *resultCharPtr = (char) (*(base16CharRepresentationPtr + (*valueCharPtr >> 4)) ^ *(keyPtr + keyIndex));
                    resultCharPtr++;
                    keyIndex++;

                    if (keyIndex == keyLength)
                    {
                        keyIndex = 0;
                    }

                    *resultCharPtr = (char) (*(base16CharRepresentationPtr + (*valueCharPtr & 15)) ^ *(keyPtr + keyIndex));
                    resultCharPtr++;
                    keyIndex++;

                    if (keyIndex == keyLength)
                    {
                        keyIndex = 0;
                    }

                    valueCharPtr++;
                }
            }

            return result;
        }

#if NETCOREAPP
        private static string ConvertStringToHexAndEncryptXorShare2(string value, string key)
        {
            var valueLength = value.Length;

            return string.Create(valueLength * 2, (value, valueLength, key, key.Length, Base16CharRepresentation), (span, state) =>
            {
                var (valueState, valueLengthState, keyState, keyLengthState, base16CharRepresentation) = state;
                var valueSpan = valueState.AsSpan();
                var keySpan = keyState.AsSpan();
                var base16CharRepresentationSpan = base16CharRepresentation.AsSpan();

                for (var i = 0; i < valueLengthState; i++)
                {
                    span[i * 2] = (char) (base16CharRepresentationSpan[valueSpan[i] >> 4] ^ keySpan[i * 2 % keyLengthState]);
                    span[i * 2 + 1] = (char) (base16CharRepresentationSpan[valueSpan[i] & 15] ^ keySpan[(i * 2 + 1) % keyLengthState]);
                }
            });
        }
#endif

        [Benchmark]
        public string ConvertStringToHexAndEncryptXorShare()
        {
            return ConvertStringToHexAndEncryptXorShare(TestData, "128");
        }

#if NETCOREAPP
        [Benchmark]
        public string ConvertStringToHexAndEncryptXorShare2()
        {
            return ConvertStringToHexAndEncryptXorShare2(TestData, "128");
        }
#endif
    }
}