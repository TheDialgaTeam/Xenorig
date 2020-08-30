using BenchmarkDotNet.Attributes;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    [Config(typeof(Config))]
    public class MathOperatorBenchmark
    {
        [Benchmark]
        public int DoSumInt()
        {
            return 1000 + 1000;
        }

        [Benchmark]
        public decimal DoDecimalInt()
        {
            return 1000m + 1000m;
        }
    }
}