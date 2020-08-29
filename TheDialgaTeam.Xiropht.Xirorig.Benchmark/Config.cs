using System.Runtime.InteropServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    public class Config : ManualConfig
    {
        public Config()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AddJob(Job.Default.WithRuntime(ClrRuntime.Net462));
                AddJob(Job.Default.WithRuntime(CoreRuntime.Core50));
            }
            else
            {
                AddJob(Job.Default.WithRuntime(MonoRuntime.Default));
            }
        }
    }
}