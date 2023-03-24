using System;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public readonly ref struct BlockHeaderStack
{
    public required int Height { get; init; }
    public required long TimestampCreate { get; init; }

    public required string Method { get; init; }
    public required string Indication { get; init; }

    public required long Difficulty { get; init; }
    public required long MinRange { get; init; }
    public required long MaxRange { get; init; }

    public required ReadOnlySpan<byte> XorKey { get; init; }

    public required ReadOnlySpan<byte> AesKey { get; init; }
    public required ReadOnlySpan<byte> AesIv { get; init; }
    
    public required int AesRound { get; init; }
}