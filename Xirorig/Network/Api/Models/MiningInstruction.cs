namespace Xirorig.Network.Api.Models
{
    internal enum MiningInstruction
    {
        DoNonceIv = 0,
        DoLz4CompressNonceIv = 1,
        DoNonceIvIterations = 2,
        DoNonceIvXor = 3,
        DoNonceIvEasySquareMath = 4,
        DoEncryptedPocShare = 5
    }
}