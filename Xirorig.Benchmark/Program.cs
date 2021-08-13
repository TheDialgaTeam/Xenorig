using BenchmarkDotNet.Running;
using Xirorig.Benchmark.Utility;

namespace Xirorig.Benchmark
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<Sha3UtilityTest>();
        }
    }
}