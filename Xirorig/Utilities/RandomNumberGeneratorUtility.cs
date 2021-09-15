// Xirorig
// Copyright 2021 Yong Jian Ming
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Xirorig.Utilities
{
    internal static class RandomNumberGeneratorUtility
    {
        public static unsafe int GetRandomBetween(int minimumValue, int maximumValue)
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

        public static unsafe long GetRandomBetween(long minimumValue, long maximumValue)
        {
            var range = (ulong) maximumValue - (ulong) minimumValue;
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
                        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(span));
                        result = maxValue & span[0];
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
                        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(span));
                        result = maxValue & span[0];
                    } while (result > range);

                    return (long) result + minimumValue;
                }
            }
        }
    }
}