using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Xenolib.Utilities;

namespace Xenorig.Options;

public sealed class XenorigOptions
{
    [UsedImplicitly]
    public int PrintSpeedDuration { get; private set; } = 10;

    [UsedImplicitly]
    public int NetworkTimeoutDuration { get; private set; } = 5;

    [UsedImplicitly]
    public int MaxRetryCount { get; private set; } = 5;

    [UsedImplicitly]
    public int DonatePercentage { get; private set; }

    [UsedImplicitly]
    public Pool[] Pools
    {
        get => _pools!;
        private set => _pools = value ?? _pools;
    }

    [UsedImplicitly]
    public XenophyteCentralizedSolo Xenophyte_Centralized_Solo
    {
        get => _xenophyteCentralizedSolo!;
        private set => _xenophyteCentralizedSolo = value ?? _xenophyteCentralizedSolo;
    }

    private Pool[]? _pools = Array.Empty<Pool>();
    private XenophyteCentralizedSolo? _xenophyteCentralizedSolo = new();
}

public sealed class Pool
{
    [UsedImplicitly]
    public string Algorithm
    {
        get => _algorithm!;
        private set => _algorithm = string.IsNullOrEmpty(value) ? _algorithm : value;
    }

    [UsedImplicitly]
    public string Coin
    {
        get => _coin!;
        private set => _coin = string.IsNullOrEmpty(value) ? _coin : value;
    }

    [UsedImplicitly]
    [StringSyntax(StringSyntaxAttribute.Uri)]
    public string Url
    {
        get => _url!;
        private set => _url = string.IsNullOrEmpty(value) ? _url : value;
    }

    [UsedImplicitly]
    public string Username
    {
        get => _username!;
        private set => _username = string.IsNullOrEmpty(value) ? _username : value;
    }

    [UsedImplicitly]
    public string Password
    {
        get => _password!;
        private set => _password = string.IsNullOrEmpty(value) ? _password : value;
    }

    [UsedImplicitly]
    public string UserAgent
    {
        get => _userAgent!;
        private set => _userAgent = string.IsNullOrEmpty(value) ? _userAgent : value;
    }

    private string? _algorithm = string.Empty;
    private string? _coin = string.Empty;
    private string? _url = string.Empty;
    private string? _username = string.Empty;
    private string? _password = string.Empty;
    private string? _userAgent = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";
}

public sealed class XenophyteCentralizedSolo
{
    [UsedImplicitly]
    public CpuMiner CpuMiner
    {
        get => _cpuMiner!;
        private set => _cpuMiner = value ?? _cpuMiner;
    }

    private CpuMiner? _cpuMiner = new();
}

public sealed class CpuMiner
{
    [UsedImplicitly]
    public int Threads { get; private set; } = Environment.ProcessorCount;

    [UsedImplicitly]
    public ThreadPriority ThreadPriority { get; private set; } = ThreadPriority.Normal;

    [UsedImplicitly]
    public CpuMinerThreadConfiguration[] ThreadConfigs
    {
        get => _threadConfigs!;
        private set => _threadConfigs = value ?? _threadConfigs;
    }

    [UsedImplicitly]
    public bool EasyBlockOnly { get; private set; }

    [UsedImplicitly]
    public bool UseXenophyteRandomizer { get; private set; }

    [UsedImplicitly]
    public int JobMinRangePercentage { get; private set; }

    [UsedImplicitly]
    public int JobMaxRangePercentage { get; private set; } = 100;

    private CpuMinerThreadConfiguration[]? _threadConfigs = Array.Empty<CpuMinerThreadConfiguration>();

    public int GetNumberOfThreads()
    {
        return Math.Max(Threads, ThreadConfigs.Length);
    }

    public ulong GetThreadAffinity(int thread)
    {
        return GetThreadConfig(thread)?.ThreadAffinity ?? 0;
    }

    public ThreadPriority GetThreadPriority(int thread)
    {
        return GetThreadConfig(thread)?.ThreadPriority ?? ThreadPriority;
    }

    public bool GetUseXenophyteRandomizer(int thread)
    {
        return GetThreadConfig(thread)?.UseXenophyteRandomizer ?? UseXenophyteRandomizer;
    }

    private CpuMinerThreadConfiguration? GetThreadConfig(int thread)
    {
        return thread < ThreadConfigs.Length ? ThreadConfigs[thread] : null;
    }
}

public sealed class CpuMinerThreadConfiguration
{
    [UsedImplicitly]
    public ulong? ThreadAffinity { get; private set; }

    [UsedImplicitly]
    public ThreadPriority? ThreadPriority { get; private set; } = System.Threading.ThreadPriority.Normal;

    [UsedImplicitly]
    public bool? UseXenophyteRandomizer { get; private set; }
}