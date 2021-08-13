using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Org.BouncyCastle.Crypto.Digests;

namespace Xirorig.Benchmark.Utility
{
    public class Sha3UtilityTest
    {
        private byte[] _data;

        [DllImport("../../../../xirorig_native")]
        private static extern unsafe int computeSha3512Hash(byte* input, int inputSize, byte* output);

        [GlobalSetup]
        public void Setup()
        {
            _data = RandomNumberGenerator.GetBytes(64);
        }

        [Benchmark]
        public byte[] Sha3_BouncyCastle()
        {
            var result = new byte[512 / 8];
            var sha3 = new Sha3Digest(512);
            sha3.BlockUpdate(_data, 0, _data.Length);
            sha3.DoFinal(result, 0);
            return result;
        }

        [Benchmark]
        public unsafe byte[] Sha3_OpenSSL()
        {
            var output = new byte[512 / 8];

            fixed (byte* inputPtr = _data, outputPtr = output)
            {
                if (computeSha3512Hash(inputPtr, _data.Length, outputPtr) == 0) throw new Exception();
            }

            return output;
        }
    }
}