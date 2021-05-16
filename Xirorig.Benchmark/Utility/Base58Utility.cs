using System.Numerics;
using System.Text;

namespace Xirorig.Benchmark.Utility
{
    public static class Base58Utility
    {
        private static readonly char[] Characters =
        {
            '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
        };

        public static string Encode(byte[] payload)
        {
            var payloadValue = new BigInteger(payload, true, true);
            var result = new StringBuilder();
            var characters = Characters;

            while (payloadValue > 0)
            {
                payloadValue = BigInteger.DivRem(payloadValue, 58, out var remainder);
                result.Insert(0, characters[(int) remainder]);
            }

            foreach (var value in payload)
            {
                if (value != 0) break;
                result.Insert(0, '1');
            }

            return result.ToString();
        }
    }
}