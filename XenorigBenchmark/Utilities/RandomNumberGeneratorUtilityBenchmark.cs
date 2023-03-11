using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Xenorig.Utilities;

namespace XenorigBenchmark.Utilities;

[SimpleJob(RuntimeMoniker.Net70)]
public class RandomNumberGeneratorUtilityBenchmark
{
    [Benchmark]
    [Arguments(int.MinValue, int.MaxValue)]
    public int GetRandomBetweenInt(int minimumValue, int maximumValue)
    {
        return RandomNumberGeneratorUtility.GetRandomBetween(minimumValue, maximumValue);
    }

    [Benchmark]
    [Arguments(long.MinValue, long.MaxValue)]
    public long GetRandomBetweenLong(long minimumValue, long maximumValue)
    {
        return RandomNumberGeneratorUtility.GetRandomBetween(minimumValue, maximumValue);
    }
}