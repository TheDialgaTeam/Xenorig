using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Xirorig.Utility
{
    internal static class RandomNumberGeneratorUtility
    {
        public static unsafe int GetRandomBetween(int minimumValue, int maximumValue)
        {
            var num1 = (uint) (maximumValue - minimumValue);
            if (num1 == 0) return minimumValue;

            var num3 = num1 | (num1 >> 1);
            var num4 = num3 | (num3 >> 2);
            var num5 = num4 | (num4 >> 4);
            var num6 = num5 | (num5 >> 8);
            var num7 = num6 | (num6 >> 16);

            Span<uint> span = stackalloc uint[1];
            uint num8;

            do
            {
                RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(span));
                num8 = num7 & span[0];
            } while (num8 > num1);

            return (int) num8 + minimumValue;
        }

        public static unsafe long GetRandomBetween(long minimumValue, long maximumValue)
        {
            var num1 = (ulong) (maximumValue - minimumValue);
            if (num1 == 0) return minimumValue;

            var num3 = num1 | (num1 >> 1);
            var num4 = num3 | (num3 >> 2);
            var num5 = num4 | (num4 >> 4);
            var num6 = num5 | (num5 >> 8);
            var num7 = num6 | (num6 >> 16);
            var num8 = num7 | (num7 >> 32);

            Span<ulong> span = stackalloc ulong[1];
            ulong num9;

            do
            {
                RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(span));
                num9 = num8 & span[0];
            } while (num9 > num1);

            return (long) num9 + minimumValue;
        }
    }
}