using System.Numerics;

namespace Xirorig.Algorithms.Xiropht.Decentralized.Api.JobResult.Models
{
    internal class MiningPowShare
    {
        public string WalletAddress { get; set; } = string.Empty;

        public long BlockHeight { get; set; }

        public string BlockHash { get; set; } = string.Empty;

        public string PoWaCShare { get; set; } = string.Empty;

        public long Nonce { get; set; }

        public string NonceComputedHexString { get; set; } = string.Empty;

        public BigInteger PoWaCShareDifficulty { get; set; }

        public long Timestamp { get; set; }
    }
}