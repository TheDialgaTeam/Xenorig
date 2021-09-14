using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

namespace Xirorig.Benchmark.Utilities
{
    public class RandomNumberGeneratorUtility
    {
        [Benchmark]
        [Arguments(0, byte.MaxValue)]
        [Arguments(0, ushort.MaxValue)]
        [Arguments(0, int.MaxValue)]
        public unsafe int GetRandomBetween_Branch(int minimumValue, int maximumValue)
        {
            var range = (uint) maximumValue - (uint) minimumValue;
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
                        result = (byte) (maxValue & span[0]);
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
                        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(span));
                        result = (ushort) (maxValue & span[0]);
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
                        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(span));
                        result = maxValue & span[0];
                    } while (result > range);

                    return (int) result + minimumValue;
                }
            }
        }

        [Benchmark]
        [Arguments(0, byte.MaxValue)]
        [Arguments(0, ushort.MaxValue)]
        [Arguments(0, int.MaxValue)]
        public unsafe int GetRandomBetween(int minimumValue, int maximumValue)
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
                result = maxValue & span[0];
            } while (result > range);

            return (int) result + minimumValue;
        }
    }
}