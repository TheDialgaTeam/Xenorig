using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Xirorig.Utility
{
    internal static class RandomNumberGeneratorUtility
    {
        public static unsafe int GetRandomBetween(int minimumValue, int maximumValue)
        {
            var range = (uint) maximumValue - (uint) minimumValue;
            if (range == 0) return minimumValue;

            var maxValue = range | (range >> 1);
            maxValue |= maxValue >> 2;
            maxValue |= maxValue >> 4;
            maxValue |= maxValue >> 8;
            maxValue |= maxValue >> 16;

            Span<uint> span = stackalloc uint[1];
            uint result;

            do
            {
                RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(span));
                result = maxValue & MemoryMarshal.GetReference(span);
            } while (result > range);

            return (int) result + minimumValue;
        }

        public static unsafe long GetRandomBetween(long minimumValue, long maximumValue)
        {
            var range = (ulong) maximumValue - (ulong) minimumValue;
            if (range == 0) return minimumValue;

            var maxValue = range | (range >> 1);
            maxValue |= maxValue >> 2;
            maxValue |= maxValue >> 4;
            maxValue |= maxValue >> 8;
            maxValue |= maxValue >> 16;
            maxValue |= maxValue >> 32;

            Span<ulong> span = stackalloc ulong[1];
            ulong result;

            do
            {
                RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(span));
                result = maxValue & MemoryMarshal.GetReference(span);
            } while (result > range);

            return (long) result + minimumValue;
        }
    }
}