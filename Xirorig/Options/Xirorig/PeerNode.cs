using Xirorig.Algorithm.Enums;

namespace Xirorig.Options.Xirorig
{
    internal class PeerNode
    {
        public string Url { get; set; }

        public AlgorithmType Algorithm { get; set; }

        public string WalletAddress { get; set; }
    }
}