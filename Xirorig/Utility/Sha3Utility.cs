using Org.BouncyCastle.Crypto.Digests;

namespace Xirorig.Utility
{
    internal static class Sha3Utility
    {
        public const int Sha3512OutputSize = 512 / 8;

        public static byte[] DoSha3512Hash(Sha3Digest sha3Digest, byte[] input)
        {
            var output = new byte[Sha3512OutputSize];
            sha3Digest.BlockUpdate(input, 0, input.Length);
            sha3Digest.DoFinal(output, 0);
            sha3Digest.Reset();
            return output;
        }

        public static void DoSha3512Hash(Sha3Digest sha3Digest, byte[] input, byte[] output)
        {
            sha3Digest.BlockUpdate(input, 0, input.Length);
            sha3Digest.DoFinal(output, 0);
            sha3Digest.Reset();
        }

        public static byte[] DoSha3512Hash(Sha3Digest sha3Digest, byte[] input, int offset, int length)
        {
            var output = new byte[Sha3512OutputSize];
            sha3Digest.BlockUpdate(input, offset, length);
            sha3Digest.DoFinal(output, 0);
            sha3Digest.Reset();
            return output;
        }

        public static void DoSha3512Hash(Sha3Digest sha3Digest, byte[] input, int offset, int length, byte[] output)
        {
            sha3Digest.BlockUpdate(input, offset, length);
            sha3Digest.DoFinal(output, 0);
            sha3Digest.Reset();
        }
    }
}