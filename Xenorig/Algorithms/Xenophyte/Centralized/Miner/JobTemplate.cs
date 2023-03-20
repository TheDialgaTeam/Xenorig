using System;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Miner;

public sealed class JobTemplate
{
    public int BlockHeight { get; set; }
    public long BlockTimestampCreate { get; set; }

    public string BlockIndication { get; set; } = string.Empty;

    public long BlockDifficulty { get; set; }
    public long BlockMinRange { get; set; }
    public long BlockMaxRange { get; set; }

    public byte[] XorKey { get; set; } = Array.Empty<byte>();
    public int XorKeyLength { get; set; }

    public byte[] AesKey { get; set; } = Array.Empty<byte>();
    public byte[] AesIv { get; set; } = Array.Empty<byte>();
    
    public int AesRound { get; set; }

    public long[] EasyBlockValues { get; set; }

    public int EasyBlockValuesLength { get; set; }

    public long[] NonEasyBlockValues { get; set; }

    public int NonEasyBlockValuesLength { get; set; }

    public long[] TempNonEasyBlockValues { get; set; }
}