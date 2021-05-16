using BenchmarkDotNet.Attributes;
using Org.BouncyCastle.Crypto.Digests;

namespace Xirorig.Benchmark.Utility
{
    public class Sha3Test
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
        public byte[] Sha3_BouncyCastle()
        {
            var result = new byte[512];
            var sha3 = new Sha3Digest(512);
            sha3.BlockUpdate(_data, 0, _data.Length);
            sha3.DoFinal(result, 0);
            return result;
        }
    }
}