using BenchmarkDotNet.Running;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<EncryptStringWithStringHexAndXorBenchmark>();
        }
    }
}