using System;
using System.Numerics;
using System.Text;

namespace Xirorig.Utility
{
    internal static class Base58Utility
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

        public static byte[] Decode(string payload)
        {
            var payloadValue = BigInteger.Zero;
            var leadingZeroCountStop = false;
            var leadingZeroCount = 0;
            var characters = Characters;

            foreach (var value in payload)
            {
                payloadValue = payloadValue * 58 + Array.IndexOf(characters, value);

                if (value != '1') leadingZeroCountStop = true;
                if (leadingZeroCountStop) continue;
                leadingZeroCount++;
            }

            if (leadingZeroCount == 0)
            {
                return payloadValue.ToByteArray(true, true);
            }

            var payloadResult = payloadValue.ToByteArray(true, true);
            var result = new byte[payloadResult.Length + leadingZeroCount];

            Array.Fill<byte>(result, 0, 0, leadingZeroCount);
            Buffer.BlockCopy(payloadResult, 0, result, leadingZeroCount, payloadResult.Length);

            return result;
        }

        public static byte[] DecodeWithoutChecksum(string payload)
        {
            var payloadValue = BigInteger.Zero;
            var leadingZeroCountStop = false;
            var leadingZeroCount = 0;
            var characters = Characters;

            foreach (var value in payload)
            {
                payloadValue = payloadValue * 58 + Array.IndexOf(characters, value);

                if (value != '1') leadingZeroCountStop = true;
                if (leadingZeroCountStop) continue;
                leadingZeroCount++;
            }

            if (leadingZeroCount == 0)
            {
                return payloadValue.ToByteArray(true, true);
            }

            var payloadResult = payloadValue.ToByteArray(true, true);
            var result = new byte[payloadResult.Length + leadingZeroCount - 16];

            Array.Fill<byte>(result, 0, 0, leadingZeroCount);
            Buffer.BlockCopy(payloadResult, 0, result, leadingZeroCount, payloadResult.Length - 16);

            return result;
        }
    }
}