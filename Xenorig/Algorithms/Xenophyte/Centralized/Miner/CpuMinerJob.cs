using Xenolib.Algorithms.Xenophyte.Centralized.Networking;
using Xenolib.Algorithms.Xenophyte.Centralized.Utilities;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Miner;

public sealed class CpuMinerJob
{
    public int BlockHeight { get; private set; }

    public long BlockTimestampCreate { get; private set; }

    public string BlockIndication { get; private set; } = string.Empty;

    public long BlockDifficulty { get; private set; }

    public long BlockMinRange { get; private set; }

    public long BlockMaxRange { get; private set; }

    public ReadOnlySpan<byte> XorKey => _xorKey.AsSpan(0, _xorKeyLength);

    public ReadOnlySpan<byte> AesKey => _aesKey.AsSpan(0, _aesKeyLength);

    public ReadOnlySpan<byte> AesIv => _aesIv.AsSpan();

    public int AesRound { get; private set; }

    public Span<long> EasyBlockValues => _easyBlockValues.AsSpan(0, _easyBlockValuesLength);

    public bool HasNewBlock { get; set; }
    public bool BlockFound { get; set; }

    private byte[] _xorKey = Array.Empty<byte>();
    private int _xorKeyLength;

    private readonly byte[] _aesKey = new byte[32];
    private int _aesKeyLength;

    private readonly byte[] _aesIv = new byte[16];

    private readonly long[] _easyBlockValues = new long[256];
    private int _easyBlockValuesLength = 256;

    public void Update(BlockHeader blockHeader)
    {
        BlockHeight = blockHeader.BlockHeight;
        BlockTimestampCreate = blockHeader.BlockTimestampCreate;
        BlockIndication = blockHeader.BlockIndication;
        BlockDifficulty = blockHeader.BlockDifficulty;
        BlockMinRange = blockHeader.BlockMinRange;
        BlockMaxRange = blockHeader.BlockMaxRange;

        if (_xorKey.Length < blockHeader.XorKey.Length)
        {
            _xorKey = GC.AllocateUninitializedArray<byte>(blockHeader.XorKey.Length);
        }

        blockHeader.XorKey.CopyTo(_xorKey);
        _xorKeyLength = blockHeader.XorKey.Length;

        blockHeader.AesKey.CopyTo(_aesKey);
        _aesKeyLength = blockHeader.AesKey.Length;

        blockHeader.AesIv.CopyTo(_aesIv);

        AesRound = blockHeader.AesRound;

        HasNewBlock = true;
    }

    public void GenerateEasyBlockValues()
    {
        _easyBlockValuesLength = CpuMinerUtility.GenerateEasyBlockNumbers(BlockMinRange, BlockMaxRange, _easyBlockValues);
    }
}