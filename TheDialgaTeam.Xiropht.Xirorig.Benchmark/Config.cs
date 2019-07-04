using System.Runtime.InteropServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    public class Config : ManualConfig
    {
        public Config()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Add(Job.Clr.With(CsProjClassicNetToolchain.Net462));
                Add(Job.Core.With(CsProjCoreToolchain.NetCoreApp30));
            }
            else
                Add(Job.Mono);
        }
    }
}