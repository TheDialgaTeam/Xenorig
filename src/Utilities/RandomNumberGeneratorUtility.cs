using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace TheDialgaTeam.Xiropht.Xirorig.Utilities;

public static class RandomNumberGeneratorUtility
{
    [SkipLocalsInit]
    public static int GetRandomBetween(int minimumValue, int maximumValue)
    {
        var range = (uint) (maximumValue - minimumValue);
        if (range == 0) return minimumValue;

        switch (range)
        {
            case <= byte.MaxValue:
            {
                var maxValue = (byte) (range | (range >> 1));
                maxValue |= (byte) (maxValue >> 2);
                maxValue |= (byte) (maxValue >> 4);

                Span<byte> span = stackalloc byte[1];
                byte result;

                do
                {
                    RandomNumberGenerator.Fill(span);
                    result = (byte) (maxValue & span.GetRef(0));
                } while (result > range);

                return result + minimumValue;
            }

            case <= ushort.MaxValue:
            {
                var maxValue = (ushort) (range | (range >> 1));
                maxValue |= (ushort) (maxValue >> 2);
                maxValue |= (ushort) (maxValue >> 4);
                maxValue |= (ushort) (maxValue >> 8);

                Span<ushort> span = stackalloc ushort[1];
                ushort result;

                do
                {
                    RandomNumberGenerator.Fill(span.AsBytes());
                    result = (ushort) (maxValue & span.GetRef(0));
                } while (result > range);

                return result + minimumValue;
            }

            default:
            {
                var maxValue = range | (range >> 1);
                maxValue |= maxValue >> 2;
                maxValue |= maxValue >> 4;
                maxValue |= maxValue >> 8;
                maxValue |= maxValue >> 16;

                Span<uint> span = stackalloc uint[1];
                uint result;

                do
                {
                    RandomNumberGenerator.Fill(span.AsBytes());
                    result = maxValue & span.GetRef(0);
                } while (result > range);

                return (int) result + minimumValue;
            }
        }
    }

    [SkipLocalsInit]
    public static long GetRandomBetween(long minimumValue, long maximumValue)
    {
        var range = (ulong) (maximumValue - minimumValue);
        if (range == 0) return minimumValue;

        switch (range)
        {
            case <= byte.MaxValue:
            {
                var maxValue = (byte) (range | (range >> 1));
                maxValue |= (byte) (maxValue >> 2);
                maxValue |= (byte) (maxValue >> 4);

                Span<byte> span = stackalloc byte[1];
                byte result;

                do
                {
                    RandomNumberGenerator.Fill(span);
                    result = (byte) (maxValue & span.GetRef(0));
                } while (result > range);

                return result + minimumValue;
            }

            case <= ushort.MaxValue:
            {
                var maxValue = (ushort) (range | (range >> 1));
                maxValue |= (ushort) (maxValue >> 2);
                maxValue |= (ushort) (maxValue >> 4);
                maxValue |= (ushort) (maxValue >> 8);

                Span<ushort> span = stackalloc ushort[1];
                ushort result;

                do
                {
                    RandomNumberGenerator.Fill(span.AsBytes());
                    result = (ushort) (maxValue & span.GetRef(0));
                } while (result > range);

                return result + minimumValue;
            }

            case <= uint.MaxValue:
            {
                var maxValue = (uint) (range | (range >> 1));
                maxValue |= maxValue >> 2;
                maxValue |= maxValue >> 4;
                maxValue |= maxValue >> 8;
                maxValue |= maxValue >> 16;

                Span<uint> span = stackalloc uint[1];
                uint result;

                do
                {
                    RandomNumberGenerator.Fill(span.AsBytes());
                    result = maxValue & span.GetRef(0);
                } while (result > range);

                return result + minimumValue;
            }

            default:
            {
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
                    RandomNumberGenerator.Fill(span.AsBytes());
                    result = maxValue & span.GetRef(0);
                } while (result > range);

                return (long) result + minimumValue;
            }
        }
    }
}