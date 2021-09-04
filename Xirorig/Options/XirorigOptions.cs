using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Xirorig.Utility;

namespace Xirorig.Options
{
    internal class MinerInstance
    {
        public Pool[]? Pools { get; set; }

        public CpuMiner? CpuMiner { get; set; }

        public Pool[] GetPools()
        {
            return Pools ?? Array.Empty<Pool>();
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
            return string.IsNullOrWhiteSpace(Username) ? throw new JsonException($"{nameof(Username)} is null.") : Username;
        }

        public string GetPassword()
        {
            return Password ?? string.Empty;
        }

        public string GetUserAgent()
        {
            return string.IsNullOrWhiteSpace(UserAgent) ? NetworkUtility.DefaultUserAgent : UserAgent;
        }
    }

    internal class CpuMiner
    {
        public int? NumberOfThreads { get; set; }

        public ThreadPriority? ThreadPriority { get; set; }

        public CpuMinerThreadConfiguration[]? Threads { get; set; }

        public int GetNumberOfThreads()
        {
            return NumberOfThreads ?? 0;
        }

        public ThreadPriority GetThreadPriority()
        {
            return ThreadPriority ?? System.Threading.ThreadPriority.Normal;
        }

        public CpuMinerThreadConfiguration[] GetThreads()
        {
            var result = new List<CpuMinerThreadConfiguration>(Threads ?? Array.Empty<CpuMinerThreadConfiguration>());

            for (var i = result.Count; i < NumberOfThreads; i++)
            {
                result.Add(new CpuMinerThreadConfiguration { ThreadPriority = GetThreadPriority() });
            }

            return result.ToArray();
        }
    }

    internal class CpuMinerThreadConfiguration
    {
        public ulong? ThreadAffinity { get; set; }

        public ThreadPriority? ThreadPriority { get; set; }

        public ulong GetThreadAffinity()
        {
            return ThreadAffinity ?? 0;
        }

        public ThreadPriority GetThreadPriority()
        {
            return ThreadPriority ?? System.Threading.ThreadPriority.Normal;
        }
    }

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
            return MinerInstances ?? Array.Empty<MinerInstance>();
        }
    }
}