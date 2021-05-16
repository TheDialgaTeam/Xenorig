namespace Xirorig.Algorithm
{
    internal interface IAlgorithm
    {
        Enums.AlgorithmType AlgorithmType { get; }

        string BlockchainVersion { get; }

        int BlockchainChecksum { get; }

        int BlockchainSha512HexStringLength { get; }

        byte[] BlockchainMarkKey { get; }
    }
}