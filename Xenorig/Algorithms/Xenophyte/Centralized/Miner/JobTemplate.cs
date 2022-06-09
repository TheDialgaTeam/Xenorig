namespace Xenorig.Algorithms.Xenophyte.Centralized.Miner;

internal ref struct JobTemplate
{
    public int BlockHeight { get; set; } = 0;

    public long BlockTimestampCreate { get; set; } = 0;

    public string BlockIndication { get; set; } = string.Empty;

    public long BlockDifficulty { get; set; } = 0;

    public long BlockMinRange { get; set; } = 0;

    public long BlockMaxRange { get; set; } = 0;

    public Span<byte> XorKey { get; set; } = Span<byte>.Empty;

    public Span<byte> AesKey { get; set; } = Span<byte>.Empty;

    public Span<byte> AesIv { get; set; } = Span<byte>.Empty;

    public int AesRound { get; set; } = 0;

    public Span<long> EasyBlockValues { get; set; } = Span<long>.Empty;

    public long[] NonEasyBlockValues { get; set; } = Array.Empty<long>();

    public long NonEasyBlockValuesLength { get; set; } = 0;

    public long[] TempNonEasyBlockValues { get; set; } = Array.Empty<long>();

    public JobTemplate()
    {
    }
}