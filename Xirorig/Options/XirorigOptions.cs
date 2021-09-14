using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Xirorig.Miner;

namespace Xirorig.Options
{
    internal class XirorigOptions
    {
        public int? PrintTime { get; set; }

        public int? MaxPingTime { get; set; }

        public int? MaxRetryCount { get; set; }

        public int? DonatePercentage { get; set; }

        public MinerInstance[]? MinerInstances { get; set; }

        public int GetPrintTime()
        {
            return PrintTime ?? 10;
        }

        public int GetMaxPingTime()
        {
            return MaxPingTime ?? 1000;
        }

        public int GetMaxRetryCount()
        {
            return MaxRetryCount ?? 10;
        }

        public int GetDonatePercentage()
        {
            return DonatePercentage ?? 0;
        }

        public MinerInstance[] GetMinerInstances()
        {
            return MinerInstances ?? throw new JsonException($"{nameof(MinerInstances)} is null.");
        }
    }

    internal class MinerInstance
    {
        public Pool[]? Pools { get; set; }

        public CpuMiner? CpuMiner { get; set; }

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
            return string.IsNullOrWhiteSpace(UserAgent) ? MinerNetwork.DefaultUserAgent : UserAgent;
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
            return Threads ?? 0;
        }

        public ThreadPriority GetThreadPriority()
        {
            return ThreadPriority ?? System.Threading.ThreadPriority.Normal;
        }

        public string GetExtraParams()
        {
            return ExtraParams ?? string.Empty;
        }

        public CpuMinerThreadConfiguration[] GetThreadConfigs()
        {
            return ThreadConfigs ?? Array.Empty<CpuMinerThreadConfiguration>();
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
}