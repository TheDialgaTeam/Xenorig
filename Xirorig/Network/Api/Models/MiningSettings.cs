using System;
using Newtonsoft.Json;

namespace Xirorig.Network.Api.Models
{
    internal class MiningSettings
    {
        public long BlockHeightStart { get; set; }

        public int PowRoundAesShare { get; set; }

        public int PocRoundShaNonce { get; set; }

        public long PocShareNonceMin { get; set; }

        public long PocShareNonceMax { get; set; }

        public int PocShareNonceMaxSquareRetry { get; set; }

        public int PocShareNonceNoSquareFoundShaRounds { get; set; }

        public int PocShareNonceIvIteration { get; set; }

        public int RandomDataShareNumberSize { get; set; }

        public int RandomDataShareTimestampSize { get; set; }

        public int RandomDataShareBlockHeightSize { get; set; }

        public int RandomDataShareChecksum { get; set; }

        public int WalletAddressDataSize { get; set; }

        public int RandomDataShareSize { get; set; }

        public int ShareHexStringSize { get; set; }

        public int ShareHexByteArraySize { get; set; }

        public string[] MathOperatorList { get; set; } = Array.Empty<string>();

        [JsonProperty("MiningIntructionsList")]
        public MiningInstruction[] MiningInstructions { get; set; } = Array.Empty<MiningInstruction>();

        public long MiningSettingTimestamp { get; set; }

        public string MiningSettingContentHash { get; set; } = string.Empty;

        public string MiningSettingContentHashSignature { get; set; } = string.Empty;

        public string MiningSettingContentDevPublicKey { get; set; } = string.Empty;
    }
}