using System;
using System.Globalization;
using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace Xirorig.Benchmark.Utility
{
    public class TestUtility
    {
        private string _testData;

        public static byte[] GetByteArrayFromHexString(string hex)
        {
            try
            {
                var ret = new byte[hex.Length / 2];

                for (var i = 0; i < ret.Length; i++)
                {
                    int high = hex[i * 2];
                    int low = hex[i * 2 + 1];
                    high = (high & 0xf) + ((high & 0x40) >> 6) * 9;
                    low = (low & 0xf) + ((low & 0x40) >> 6) * 9;

                    ret[i] = (byte) ((high << 4) | low);
                }

                return ret;
            }
            catch
            {
                return null;
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            _testData = "1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF";
        }

        [Benchmark]
        public byte[] Custom()
        {
            return GetByteArrayFromHexString(_testData);
        }

        [Benchmark]
        public byte[] FromHexStringInternal()
        {
            return Convert.FromHexString(_testData);
        }

        [Benchmark]
        public byte[] BigIntegerInternal()
        {
            return BigInteger.Parse(_testData, NumberStyles.HexNumber).ToByteArray(true, true);
        }
    }
}