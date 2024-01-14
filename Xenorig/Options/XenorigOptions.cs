using System.Diagnostics.CodeAnalysis;
using Xenolib.Utilities;

namespace Xenorig.Options;

internal sealed class XenorigOptions
{
    public int PrintSpeedDuration { get; set; } = 10;
    
    public int NetworkTimeoutDuration { get; set; } = 5;
    
    public int MaxRetryCount { get; set; } = 5;
    
    public int DonatePercentage { get; set; }
    
    public Pool[] Pools { get; set; } = Array.Empty<Pool>();

    public XenophyteCentralizedSolo Xenophyte_Centralized_Solo { get; set; } = new();
}

internal sealed class Pool
{
    public string Algorithm { get; set; } = string.Empty;
    
    public string Coin { get; set; } = string.Empty;
    
    [StringSyntax(StringSyntaxAttribute.Uri)]
    public string Url { get; set; } = string.Empty;
    
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
    
    public string UserAgent { get; set; } = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";
}

internal sealed class XenophyteCentralizedSolo
{
    public CpuMiner CpuMiner { get; set; } = new();
}

internal sealed class CpuMiner
{
    public int Threads { get; set; } = Environment.ProcessorCount;
    
    public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;

    public CpuMinerThreadConfiguration[] ThreadConfigs { get; set; } = Array.Empty<CpuMinerThreadConfiguration>();
    
    public bool DoEasyBlock { get; set; } = true;
    
    public bool UseXenophyteRandomizer { get; set; } = true;

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

    public bool GetDoEasyBlock(int thread)
    {
        return GetThreadConfig(thread)?.DoEasyBlock ?? DoEasyBlock;
    }
    
    public int GetEasyBlockIndex(int thread)
    {
        var index = -1;

        for (var i = 0; i <= thread; i++)
        {
            if (GetDoEasyBlock(thread))
            {
                index++;
            }
        }
        
        return index;
    }

    public int GetTotalEasyBlockThreads()
    {
        var result = 0;
        var numberOfThreads = GetNumberOfThreads();
        
        for (var i = 0; i < numberOfThreads; i++)
        {
            if (GetDoEasyBlock(i))
            {
                result++;
            }
        }
        
        return result;
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

internal sealed class CpuMinerThreadConfiguration
{
    public ulong? ThreadAffinity { get; set; }
    
    public ThreadPriority? ThreadPriority { get; set; } = System.Threading.ThreadPriority.Normal;
    
    public bool? DoEasyBlock { get; set; }
    
    public bool? UseXenophyteRandomizer { get; set; }
}