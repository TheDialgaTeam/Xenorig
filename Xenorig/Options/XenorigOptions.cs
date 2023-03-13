using System;
using System.Threading;
using Xenorig.Utilities;

namespace Xenorig.Options;

public sealed class XenorigOptions
{
    public int PrintSpeedDuration { get; set; } = 10;

    public int NetworkTimeoutDuration { get; set; } = 5;

    public int MaxRetryCount { get; set; } = 5;

    public int DonatePercentage { get; set; } = 0;
    
    public Pool[] Pools { get; set; } = Array.Empty<Pool>();
    
    public XenophyteCentralizedSolo Xenophyte_Centralized_Solo { get; set; } = new();
}

public sealed class Pool
{
    public string Algorithm { get; set; } = string.Empty;

    public string Coin { get; set; } = string.Empty;
    
    public string Url { get; set; } = string.Empty;
    
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string UserAgent { get; set; } = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";

    public string GetUserAgent()
    {
        return string.IsNullOrEmpty(UserAgent) ? $"{ApplicationUtility.Name}/{ApplicationUtility.Version}" : UserAgent;
    }
}

public sealed class XenophyteCentralizedSolo
{
    public CpuMiner CpuMiner { get; set; } = new();
}

public sealed class CpuMiner
{
    public int Threads { get; } = Environment.ProcessorCount;

    public ThreadPriority ThreadPriority { get; } = ThreadPriority.Normal;
    
    public CpuMinerThreadConfiguration[] ThreadConfigs { get; } = Array.Empty<CpuMinerThreadConfiguration>();

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
    public ulong ThreadAffinity { get; set; }

    public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;
}
