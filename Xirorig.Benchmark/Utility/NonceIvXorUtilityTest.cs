using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
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
            var pocShareIvLength = _testData.Length;
            var pocShareIvMath = new byte[pocShareIvLength];

            var vectorSize = Vector<byte>.Count;
            var iterationLength = pocShareIvLength - vectorSize;
            var i = 0;

            var pocShareIvSpan = _testData.AsSpan();
            Span<byte> pocShareIvReverseSpan = stackalloc byte[pocShareIvLength];
            pocShareIvSpan.CopyTo(pocShareIvReverseSpan);
            pocShareIvReverseSpan.Reverse();

            for (; i <= iterationLength; i += vectorSize)
            {
                var test = new Vector<byte>(pocShareIvSpan.Slice(i, vectorSize));
                var test2 = new Vector<byte>(pocShareIvReverseSpan.Slice(i, vectorSize));
                Vector.Xor(test, test2).CopyTo(pocShareIvMath, i);
            }

            for (; i < pocShareIvLength; i++)
            {
                pocShareIvMath[i] = (byte) (pocShareIvSpan[i] ^ pocShareIvReverseSpan[i]);
            }

            return pocShareIvMath;
        }
    }
}