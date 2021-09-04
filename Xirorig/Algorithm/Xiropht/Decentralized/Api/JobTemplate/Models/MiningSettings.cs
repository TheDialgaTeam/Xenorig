using System.Text.Json.Serialization;

namespace Xirorig.Algorithm.Xiropht.Decentralized.Api.JobTemplate.Models
{
    internal record MiningSettings(
        int PowRoundAesShare,
        int PocRoundShaNonce,
        long PocShareNonceMin,
        long PocShareNonceMax,
        int PocShareNonceMaxSquareRetry,
        int PocShareNonceNoSquareFoundShaRounds,
        int PocShareNonceIvIteration,
        int RandomDataShareChecksum,
        int WalletAddressDataSize,
        int RandomDataShareSize,
        string[] MathOperatorList,
        [property: JsonPropertyName("MiningIntructionsList")]
        MiningInstruction[] MiningInstructions
    );
}