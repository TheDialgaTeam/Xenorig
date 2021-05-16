using BenchmarkDotNet.Attributes;

namespace Xirorig.Benchmark.Utility
{
    public class Base58UtilityTest
    {
        private byte[] _data;

        [GlobalSetup]
        public void Setup()
        {
            _data = new byte[50];

            for (byte i = 0; i < 50; i++)
            {
                _data[i] = i;
            }
        }

        [Benchmark]
        public string Base58EncodeTest()
        {
            return Base58Utility.Encode(_data);
        }
    }
}