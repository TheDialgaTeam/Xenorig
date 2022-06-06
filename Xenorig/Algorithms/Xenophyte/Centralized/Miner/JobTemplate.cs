namespace Xenorig.Algorithms.Xenophyte.Centralized.Miner;

internal ref struct JobTemplate
{
    public int BlockHeight { get; set; } = 0;

    public long BlockTimestampCreate { get; set; } = 0;

    public string BlockIndication { get; set; } = string.Empty;

    public ulong BlockDifficulty { get; set; } = 0;

    public long BlockMinRange { get; set; } = 0;

    public long BlockMaxRange { get; set; } = 0;

    public Span<byte> XorKey { get; set; } = Span<byte>.Empty;

    public int XorKeyLength { get; set; } = 0;

    public Span<byte> AesPassword { get; set; } = Span<byte>.Empty;

    public int AesPasswordLength { get; set; } = 0;

    public Span<byte> AesSalt { get; set; } = Span<byte>.Empty;

    public int AesSaltLength { get; set; } = 0;

    public Span<byte> AesKey { get; set; } = Span<byte>.Empty;

    public int AesKeyLength { get; set; } = 0;

    public Span<byte> AesIv { get; } = new byte[16];

    public int AesRound { get; set; } = 0;

    public Span<long> EasyBlockValues { get; } = new long[256];

    public int EasyBlockValuesLength { get; set; } = 0;

    public Span<long> NonEasyBlockValues { get; set; } = Span<long>.Empty;

    public int NonEasyBlockValuesLength { get; set; } = 0;

    public JobTemplate()
    {
    }
}