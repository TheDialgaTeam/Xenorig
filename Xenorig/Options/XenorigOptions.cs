using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Xenorig.Utilities;

namespace Xenorig.Options;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
internal class XenorigOptions
{
    public int? PrintTime { get; set; }

    public int? MaxRetryCount { get; set; }

    public int? DonatePercentage { get; set; }

    public Pool[]? Pools { get; set; }

    public CpuMiner? CpuMiner { get; set; }

    public int GetPrintTime()
    {
        return PrintTime ?? 10;
    }

    public int GetMaxRetryCount()
    {
        return MaxRetryCount ?? 10;
    }

    public int GetDonatePercentage()
    {
        return DonatePercentage ?? 0;
    }

    public Pool[] GetPools()
    {
        return Pools ?? throw new JsonException($"{nameof(Pools)} is null.");
    }

    public CpuMiner GetCpuMiner()
    {
        return CpuMiner ?? new CpuMiner();
    }
}

internal class Pool
{
    private static readonly string DefaultUserAgent = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";

    public string? Algorithm { get; set; }

    public string? Coin { get; set; }

    public string? Url { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? UserAgent { get; set; }

    public bool? Daemon { get; set; }

    public string GetAlgorithm()
    {
        return string.IsNullOrWhiteSpace(Algorithm) ? throw new JsonException($"{nameof(Algorithm)} is null or empty.") : Algorithm;
    }

    public string GetCoin()
    {
        return Coin ?? string.Empty;
    }

    public string GetUrl()
    {
        return string.IsNullOrWhiteSpace(Url) ? throw new JsonException($"{nameof(Url)} is null or empty.") : Url;
    }

    public string GetUsername()
    {
        return string.IsNullOrWhiteSpace(Username) ? throw new JsonException($"{nameof(Username)} is null or empty.") : Username;
    }

    public string GetPassword()
    {
        return Password ?? string.Empty;
    }

    public string GetUserAgent()
    {
        return string.IsNullOrWhiteSpace(UserAgent) ? DefaultUserAgent : UserAgent;
    }

    public bool GetIsDaemon()
    {
        return Daemon ?? false;
    }
}

internal class CpuMiner
{
    public int? Threads { get; set; }

    public ThreadPriority? ThreadPriority { get; set; }

    public string? ExtraParams { get; set; }

    public CpuMinerThreadConfiguration[]? ThreadConfigs { get; set; }

    public int GetNumberOfThreads()
    {
        return Math.Max(Threads ?? 0, ThreadConfigs?.Length ?? 0);
    }

    public ulong GetThreadAffinity(int thread)
    {
        return GetThreadConfig(thread)?.GetThreadAffinity() ?? 0;
    }

    public ThreadPriority GetThreadPriority(int thread)
    {
        return GetThreadConfig(thread)?.GetThreadPriority() ?? ThreadPriority ?? System.Threading.ThreadPriority.Normal;
    }

    public string GetExtraParams(int thread)
    {
        return GetThreadConfig(thread)?.GetExtraParams() ?? ExtraParams ?? string.Empty;
    }

    private CpuMinerThreadConfiguration? GetThreadConfig(int thread)
    {
        return thread < ThreadConfigs?.Length ? ThreadConfigs[thread] : null;
    }
}

internal class CpuMinerThreadConfiguration
{
    public ulong? ThreadAffinity { get; set; }

    public ThreadPriority? ThreadPriority { get; set; }

    public string? ExtraParams { get; set; }

    public ulong GetThreadAffinity()
    {
        return ThreadAffinity ?? 0;
    }

    public ThreadPriority GetThreadPriority()
    {
        return ThreadPriority ?? System.Threading.ThreadPriority.Normal;
    }

    public string GetExtraParams()
    {
        return ExtraParams ?? string.Empty;
    }
}