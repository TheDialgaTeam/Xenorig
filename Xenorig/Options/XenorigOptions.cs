using System;
using System.Threading;
using Xenorig.Utilities;

namespace Xenorig.Options;

public sealed class XenorigOptions
{
    public int PrintSpeedDuration { get; init; } = 10;

    public int NetworkTimeoutDuration { get; init; } = 5;

    public int MaxRetryCount { get; init; } = 5;

    public int DonatePercentage { get; init; } = 0;
    
    public Pool[] Pools { get; init; } = Array.Empty<Pool>();
    
    public XenophyteCentralizedSolo Xenophyte_Centralized_Solo { get; init; } = new();
}

public sealed class Pool
{
    public string Algorithm { get; init; } = string.Empty;

    public string Coin { get; init; } = string.Empty;
    
    public string Url { get; init; } = string.Empty;
    
    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string UserAgent { get; init; } = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";

    public string GetUserAgent()
    {
        return string.IsNullOrEmpty(UserAgent) ? $"{ApplicationUtility.Name}/{ApplicationUtility.Version}" : UserAgent;
    }
}

public sealed class XenophyteCentralizedSolo
{
    public CpuMiner CpuMiner { get; init; } = new();
}

public sealed class CpuMiner
{
    public int Threads { get; init; } = Environment.ProcessorCount;

    public ThreadPriority ThreadPriority { get; init; } = ThreadPriority.Normal;
    
    public CpuMinerThreadConfiguration[] ThreadConfigs { get; init; } = Array.Empty<CpuMinerThreadConfiguration>();

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

    private CpuMinerThreadConfiguration? GetThreadConfig(int thread)
    {
        return thread < ThreadConfigs.Length ? ThreadConfigs[thread] : null;
    }
}

public sealed class CpuMinerThreadConfiguration
{
    public ulong ThreadAffinity { get; init; }

    public ThreadPriority ThreadPriority { get; init; } = ThreadPriority.Normal;
}
