using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Xenorig.Utilities;

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

    [SkipLocalsInit]
    public static long GetBiasRandomBetween(long minimumValue, long maximumValue)
    {
        var range = (ulong) (maximumValue - minimumValue);
        if (range == 0) return minimumValue;

        Span<char> minimumValueString = stackalloc char[19];
        minimumValue.TryFormat(minimumValueString, out var minimumValueLength);

        var randomMaxValue = GetRandomBetweenSize(minimumValue, maximumValue);

        Span<char> maximumValueString = stackalloc char[19];
        randomMaxValue.TryFormat(maximumValueString, out var maximumValueLength);

        var randomLength = GetRandomBetweenSize(minimumValueLength, maximumValueLength);

        switch (randomLength)
        {
            case 1:
            {
                long result;

                do
                {
                    result = GetRandomBetweenSize(0, 9);
                } while (result < minimumValue || result > maximumValue);

                return result;
            }

            case 2:
            {
                long result;

                do
                {
                    result = GetRandomBetweenSize(0, 9);
                    result += GetRandomBetweenSize(0, 9) * 10;
                } while (result < minimumValue || result > maximumValue);

                return result;
            }
            
            case 3:
            {
                long result;

                do
                {
                    result = GetRandomBetweenSize(0, 9);
                    result += GetRandomBetweenSize(0, 9) * 10;
                    result += GetRandomBetweenSize(0, 9) * 100;
                } while (result < minimumValue || result > maximumValue);

                return result;
            }
            
            case 4:
            {
                long result;

                do
                {
                    result = GetRandomBetweenSize(0, 9);
                    result += GetRandomBetweenSize(0, 9) * 10;
                    result += GetRandomBetweenSize(0, 9) * 100;
                    result += GetRandomBetweenSize(0, 9) * 1000;
                } while (result < minimumValue || result > maximumValue);

                return result;
            }
            
            case 5:
            {
                long result;

                do
                {
                    result = GetRandomBetweenSize(0, 9);
                    result += GetRandomBetweenSize(0, 9) * 10;
                    result += GetRandomBetweenSize(0, 9) * 100;
                    result += GetRandomBetweenSize(0, 9) * 1000;
                    result += GetRandomBetweenSize(0, 9) * 10000;
                } while (result < minimumValue || result > maximumValue);

                return result;
            }
            
            case 6:
            {
                long result;

                do
                {
                    result = GetRandomBetweenSize(0, 9);
                    result += GetRandomBetweenSize(0, 9) * 10;
                    result += GetRandomBetweenSize(0, 9) * 100;
                    result += GetRandomBetweenSize(0, 9) * 1000;
                    result += GetRandomBetweenSize(0, 9) * 10000;
                    result += GetRandomBetweenSize(0, 9) * 100000;
                } while (result < minimumValue || result > maximumValue);

                return result;
            }
            
            case 7:
            {
                long result;

                do
                {
                    result = GetRandomBetweenSize(0, 9);
                    result += GetRandomBetweenSize(0, 9) * 10;
                    result += GetRandomBetweenSize(0, 9) * 100;
                    result += GetRandomBetweenSize(0, 9) * 1000;
                    result += GetRandomBetweenSize(0, 9) * 10000;
                    result += GetRandomBetweenSize(0, 9) * 100000;
                    result += GetRandomBetweenSize(0, 9) * 1000000;
                } while (result < minimumValue || result > maximumValue);

                return result;
            }
            
            case 8:
            {
                long result;

                do
                {
                    result = GetRandomBetweenSize(0, 9);
                    result += GetRandomBetweenSize(0, 9) * 10;
                    result += GetRandomBetweenSize(0, 9) * 100;
                    result += GetRandomBetweenSize(0, 9) * 1000;
                    result += GetRandomBetweenSize(0, 9) * 10000;
                    result += GetRandomBetweenSize(0, 9) * 100000;
                    result += GetRandomBetweenSize(0, 9) * 1000000;
                    result += GetRandomBetweenSize(0, 9) * 10000000;
                } while (result < minimumValue || result > maximumValue);

                return result;
            }

            default:
            {
                long result;

                do
                {
                    result = 0;
                    var multiplier = 1;

                    for (var i = randomLength - 1; i >= 0; i--)
                    {
                        result += GetRandomBetweenSize(0, 9) * multiplier;
                        multiplier *= 10;
                    }
                } while (result < minimumValue || result > maximumValue);

                return result;
            }
        }
    }

    [SkipLocalsInit]
    private static int GetRandomBetweenSize(int minimumValue, int maximumValue)
    {
        Span<byte> randomIndex = stackalloc byte[1];
        RandomNumberGenerator.Fill(randomIndex);

        var multiplier = Math.Max(0, randomIndex.GetRef(0) / 255.0 - 0.00000000001);
        var range = maximumValue - minimumValue + 1;

        return minimumValue + (int) Math.Floor(multiplier * range);
    }

    [SkipLocalsInit]
    private static long GetRandomBetweenSize(long minimumValue, long maximumValue)
    {
        Span<byte> randomIndex = stackalloc byte[1];
        RandomNumberGenerator.Fill(randomIndex);

        var multiplier = Math.Max(0, randomIndex.GetRef(0) / 255.0 - 0.00000000001);
        var range = maximumValue - minimumValue + 1;

        return minimumValue + (long) Math.Floor(multiplier * range);
    }
}