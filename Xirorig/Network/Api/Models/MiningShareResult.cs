namespace Xirorig.Network.Api.Models
{
    internal enum MiningShareResult
    {
        EmptyShare = 0,
        InvalidWalletAddress = 1,
        InvalidBlockHash = 2,
        InvalidBlockHeight = 3,
        InvalidNonceShare = 4,
        InvalidShareFormat = 5,
        InvalidShareDifficulty = 6,
        InvalidShareEncryption = 8,
        InvalidShareData = 9,
        InvalidShareDataSize = 10,
        InvalidShareCompatibility = 11,
        InvalidTimestampShare = 12,
        LowDifficultyShare = 13,
        BlockAlreadyFound = 14,
        ValidShare = 15,
        ValidUnlockBlockShare = 16
    }
}