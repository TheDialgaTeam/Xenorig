using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace Xirorig.Benchmark.Utility
{
    public class NonceIvXorUtilityTest
    {
        private byte[] _testData;

        [GlobalSetup]
        public void SetUp()
        {
            _testData = new byte[64];

            for (var i = 0; i < 64; i++)
            {
                _testData[i] = (byte) (i % 256);
            }
        }

        [Benchmark]
        public byte[] Original()
        {
            var testData = _testData;
            var testDataLength = testData.Length;
            var pocShareIvMath = new byte[testDataLength];

            for (var i = 0; i < testDataLength; i++)
            {
                pocShareIvMath[i] = (byte) (testData[i] ^ testData[^(i + 1)]);
            }

            return pocShareIvMath;
        }

        [Benchmark]
        public unsafe byte[] Vectorized()
        {
            var testData = _testData.AsSpan();
            var testDataLength = testData.Length;
            Span<byte> testDataReverse = stackalloc byte[testDataLength];
            testData.CopyTo(testDataReverse);
            testDataReverse.Reverse();

            var pocShareIvMath = new byte[testDataLength];

            var i = 0;
            var vectorCount = Vector<byte>.Count;
            var iterationSize = testDataLength / vectorCount * vectorCount;

            for (; i < iterationSize; i += vectorCount)
            {
                var test = new Vector<byte>(testData.Slice(i, vectorCount));
                var test2 = new Vector<byte>(testDataReverse.Slice(i, vectorCount));
                Vector.Xor(test, test2).CopyTo(pocShareIvMath, i);
            }

            for (; i < testDataLength; i++)
            {
                pocShareIvMath[i] = (byte) (testData[i] ^ testDataReverse[i]);
            }

            return pocShareIvMath;
        }
    }
}