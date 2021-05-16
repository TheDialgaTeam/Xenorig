using System;
using System.Security.Cryptography;
using Xirorig.Algorithm;

namespace Xirorig.Utility
{
    internal static class WalletUtility
    {
        public static bool IsValidWalletAddress(string walletAddress, IAlgorithm algorithm)
        {
            var base58RawBytes = Base58Utility.Decode(walletAddress);
            if (Convert.ToHexString(base58RawBytes, 0, 1) != algorithm.BlockchainVersion) return false;

            var givenChecksumBytes = base58RawBytes.AsSpan(base58RawBytes.Length - algorithm.BlockchainChecksum);

            using var sha256 = SHA256.Create();
            var hash1 = sha256.ComputeHash(base58RawBytes, 0, base58RawBytes.Length - algorithm.BlockchainChecksum);
            var hash2 = sha256.ComputeHash(hash1);

            return givenChecksumBytes.SequenceEqual(hash2.AsSpan(0, algorithm.BlockchainChecksum));
        }
    }
}