using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    public class Config : ManualConfig
    {
        public Config()
        {
            Add(Job.Clr.With(CsProjClassicNetToolchain.Net461));
            Add(Job.Clr.With(CsProjClassicNetToolchain.Net462));
            Add(Job.Mono.With(CsProjClassicNetToolchain.Net461));
            Add(Job.Mono.With(CsProjClassicNetToolchain.Net462));
            Add(Job.Core.With(CsProjCoreToolchain.NetCoreApp21));
            Add(Job.Core.With(CsProjCoreToolchain.NetCoreApp30));
        }
    }
}