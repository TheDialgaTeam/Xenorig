using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using TheDialgaTeam.Xiropht.Xirorig.Miner;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    [Config(typeof(Config))]
    public class GenerateNumberMathCalculationBenchmark
    {
        private readonly RNGCryptoServiceProvider _rngCryptoServiceProvider = new RNGCryptoServiceProvider();

        private readonly byte[] _randomNumber = new byte[1];

        [Benchmark]
        public float GenerateNumberMathCalculation()
        {
            return MiningUtility.GenerateNumberMathCalculation(_rngCryptoServiceProvider, _randomNumber, 2, 1000000, 1, 7);
        }
    }
}