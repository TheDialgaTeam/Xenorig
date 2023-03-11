using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using Xenorig.Utilities;

namespace Xenorig.Options;

public class XenorigOptions
{
    [Range(0, int.MaxValue)]
    public int PrintSpeedDuration { get; set; } = 10;

    [Range(0, int.MaxValue)]
    public int NetworkTimeoutDuration { get; set; } = 5000;

    [Range(0, int.MaxValue)]
    public int MaxRetryCount { get; set; } = 5;

    [Range(0, 100)]
    public int DonatePercentage { get; set; }
    
    public Pool[] Pools { get; set; } = Array.Empty<Pool>();
    
    public CpuMiner CpuMiner { get; set; } = new();
}

public class Pool
{
    [Required]
    public string Algorithm { get; set; } = string.Empty;

    public string Coin { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string UserAgent { get; set; }
    
    public bool Daemon { get; set; } = false;

    public string GetUserAgent()
    {
        return string.IsNullOrEmpty(UserAgent) ? $"{ApplicationUtility.Name}/{ApplicationUtility.Version}" : UserAgent;
    }
}

public class CpuMiner
{
    [Range(1, int.MaxValue)]
    public int Threads { get; set; } = Environment.ProcessorCount;

    public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;

    public string ExtraParams { get; set; } = string.Empty;

    public CpuMinerThreadConfiguration[] ThreadConfigs { get; set; } = Array.Empty<CpuMinerThreadConfiguration>();

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

    public string GetExtraParams(int thread)
    {
        return GetThreadConfig(thread)?.ExtraParams ?? ExtraParams;
    }

    private CpuMinerThreadConfiguration? GetThreadConfig(int thread)
    {
        return thread < ThreadConfigs.Length ? ThreadConfigs[thread] : null;
    }
}

public class CpuMinerThreadConfiguration
{
    [Range(0, ulong.MaxValue)]
    public ulong ThreadAffinity { get; set; }

    public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;

    public string ExtraParams { get; set; } = string.Empty;
}