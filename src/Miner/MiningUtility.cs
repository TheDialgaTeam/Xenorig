using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner
{
    public static class MiningUtility
    {
        private static readonly char[] Base16CharRepresentation = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static readonly byte[] Base16ByteRepresentation = { (byte) '0', (byte) '1', (byte) '2', (byte) '3', (byte) '4', (byte) '5', (byte) '6', (byte) '7', (byte) '8', (byte) '9', (byte) 'A', (byte) 'B', (byte) 'C', (byte) 'D', (byte) 'E', (byte) 'F' };

        public static unsafe string MakeEncryptedShare(string value, string xorKey, int round, ICryptoTransform aesCryptoTransform, SHA512 sha512)
        {
            var valueLength = value.Length;
            var xorKeyLength = xorKey.Length;

            var sharedArrayPool = ArrayPool<byte>.Shared;
            var outputLength = valueLength * 2;
            var output = sharedArrayPool.Rent(outputLength);

            fixed (char* base16CharRepresentationPtr = Base16CharRepresentation, xorKeyPtr = xorKey)
            fixed (byte* base16ByteRepresentationPtr = Base16ByteRepresentation)
            {
                var xorKeyIndex = 0;

                // First encryption phase convert to hex and xor each result.
                fixed (byte* outputPtr = output)
                fixed (char* valuePtr = value)
                {
                    var outputBytePtr = outputPtr;
                    var valueCharPtr = valuePtr;

                    for (var i = valueLength - 1; i >= 0; i--)
                    {
                        *outputBytePtr = (byte) (*(base16ByteRepresentationPtr + (*valueCharPtr >> 4)) ^ *(xorKeyPtr + xorKeyIndex));
                        outputBytePtr++;
                        xorKeyIndex++;

                        if (xorKeyIndex == xorKeyLength)
                        {
                            xorKeyIndex = 0;
                        }

                        *outputBytePtr = (byte) (*(base16ByteRepresentationPtr + (*valueCharPtr & 15)) ^ *(xorKeyPtr + xorKeyIndex));
                        outputBytePtr++;
                        xorKeyIndex++;

                        if (xorKeyIndex == xorKeyLength)
                        {
                            xorKeyIndex = 0;
                        }

                        valueCharPtr++;
                    }
                }

                // Second encryption phase: run through aes per round and apply xor at the final round.
                const byte dash = (byte) '-';

                for (var i = round; i >= 0; i--)
                {
                    var aesOutput = aesCryptoTransform.TransformFinalBlock(output, 0, outputLength);
                    var aesOutputLength = aesOutput.Length;

                    sharedArrayPool.Return(output);

                    outputLength = aesOutputLength * 2 + aesOutputLength - 1;
                    output = sharedArrayPool.Rent(outputLength);

                    fixed (byte* outputPtr = output, aesOutputPtr = aesOutput)
                    {
                        var outputBytePtr = outputPtr;
                        var aesOutputBytePtr = aesOutputPtr;

                        if (i == 1)
                        {
                            xorKeyIndex = 0;

                            for (var j = aesOutputLength - 1; j >= 0; j--)
                            {
                                *outputBytePtr = (byte) (*(base16ByteRepresentationPtr + (*aesOutputBytePtr >> 4)) ^ *(xorKeyPtr + xorKeyIndex));
                                outputBytePtr++;
                                xorKeyIndex++;

                                if (xorKeyIndex == xorKeyLength)
                                {
                                    xorKeyIndex = 0;
                                }

                                *outputBytePtr = (byte) (*(base16ByteRepresentationPtr + (*aesOutputBytePtr & 15)) ^ *(xorKeyPtr + xorKeyIndex));
                                outputBytePtr++;
                                xorKeyIndex++;

                                if (xorKeyIndex == xorKeyLength)
                                {
                                    xorKeyIndex = 0;
                                }

                                if (j == 0) break;

                                *outputBytePtr = (byte) (dash ^ *(xorKeyPtr + xorKeyIndex));
                                outputBytePtr++;
                                xorKeyIndex++;

                                if (xorKeyIndex == xorKeyLength)
                                {
                                    xorKeyIndex = 0;
                                }

                                aesOutputBytePtr++;
                            }
                        }
                        else
                        {
                            for (var j = aesOutputLength - 1; j >= 0; j--)
                            {
                                *outputBytePtr = *(base16ByteRepresentationPtr + (*aesOutputBytePtr >> 4));
                                outputBytePtr++;

                                *outputBytePtr = *(base16ByteRepresentationPtr + (*aesOutputBytePtr & 15));
                                outputBytePtr++;

                                if (j == 0) break;

                                *outputBytePtr = dash;
                                outputBytePtr++;
                                aesOutputBytePtr++;
                            }
                        }
                    }
                }

                // Third encryption phase: compute hash
                var hashOutput = sha512.ComputeHash(output, 0, outputLength);
                var hashOutputLength = hashOutput.Length;

                sharedArrayPool.Return(output);

                var result = new string('\0', hashOutputLength * 2);

                fixed (byte* hashOutputPtr = hashOutput)
                fixed (char* resultPtr = result)
                {
                    var hashOutputBytePtr = hashOutputPtr;
                    var resultCharPtr = resultPtr;

                    for (var i = hashOutputLength - 1; i >= 0; i--)
                    {
                        *resultCharPtr = *(base16CharRepresentationPtr + (*hashOutputBytePtr >> 4));
                        resultCharPtr++;

                        *resultCharPtr = *(base16CharRepresentationPtr + (*hashOutputBytePtr & 15));
                        resultCharPtr++;

                        hashOutputBytePtr++;
                    }
                }

                return result;
            }
        }

        public static unsafe string ComputeHash(SHA512 hashAlgorithm, string value)
        {
            var output = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            var outputLength = output.Length;
            var result = new string('\0', outputLength * 2);

            fixed (byte* outputPtr = output)
            fixed (char* base16CharRepresentationPtr = Base16CharRepresentation, resultPtr = result)
            {
                var outputBytePtr = outputPtr;
                var resultCharPtr = resultPtr;

                for (var i = outputLength - 1; i >= 0; i--)
                {
                    *resultCharPtr = *(base16CharRepresentationPtr + (*outputBytePtr >> 4));
                    resultCharPtr++;

                    *resultCharPtr = *(base16CharRepresentationPtr + (*outputBytePtr & 15));
                    resultCharPtr++;

                    outputBytePtr++;
                }
            }

            return result;
        }

        public static decimal GenerateNumberMathCalculation(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumber, decimal minRange, decimal maxRange, int minSize, int maxSize)
        {
            do
            {
                var randomSize = GetRandomBetween(rngCryptoServiceProvider, randomNumber, minSize, maxSize);

                if (randomSize == 1)
                {
                    return GetRandomBetween(rngCryptoServiceProvider, randomNumber, decimal.ToInt32(minRange), 9);
                }

                if (randomSize < 10)
                {
                    var result = 0;
                    var digit = 1;

                    for (var i = randomSize - 1; i >= 0; i--)
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumber, digit == 1 ? 1 : 0, 9);
                        digit *= 10;
                    }

                    if (result > minRange && result < maxRange) return result;
                }
                else
                {
                    var result = 0m;
                    var digit = 1m;

                    for (var i = randomSize - 1; i >= 0; i--)
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumber, digit == 1 ? 1 : 0, 9);
                        digit *= 10m;
                    }

                    if (result > minRange && result < maxRange) return result;
                }
            } while (true);
        }

        public static (decimal, decimal) GetJobRange(decimal totalPossibilities, int totalThread, int threadIndex, decimal offset)
        {
            var startRange = DivideEvenly(totalPossibilities, totalThread).Take(threadIndex + 1).Sum() - DivideEvenly(totalPossibilities, totalThread).ElementAt(threadIndex) + offset;
            var endRange = DivideEvenly(totalPossibilities, totalThread).Take(threadIndex + 1).Sum() + offset - 1;

            return (startRange, endRange);
        }

        public static (decimal, decimal) GetJobRangeByPercentage(decimal minRange, decimal maxRange, int minRangePercentage, int maxRangePercentage)
        {
            var startRange = Math.Floor(maxRange * minRangePercentage * 0.01m) + minRange;
            var endRange = Math.Min(maxRange, Math.Floor(maxRange * maxRangePercentage * 0.01m) + 1);

            return (startRange, endRange);
        }

        public static bool IsPrimeNumber(decimal number)
        {
            if (number <= 1)
            {
                return false;
            }

            if (number == 2)
            {
                return true;
            }

            if (number % 2 == 0)
            {
                return false;
            }

            var boundary = Math.Floor(SquareRoot(number));

            for (var i = 3m; i <= boundary; i += 2)
            {
                if (number % i == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<(decimal, decimal)> SumOf(decimal result, decimal minRange)
        {
            if (result == 2 || result == 3)
            {
                yield break;
            }

            for (var i = minRange; i < result - 1; i++)
            {
                var number = result - i;

                yield return (i, result - i);

                if (i != number)
                {
                    yield return (result - i, i);
                }
            }
        }

        public static IEnumerable<Tuple<decimal, decimal>> SubtractOf(decimal result, decimal maxRange)
        {
            for (var i = maxRange; i > result + 1; i--)
            {
                yield return new Tuple<decimal, decimal>(i, i - result);
            }
        }

        public static IEnumerable<(decimal, decimal)> FactorOf(decimal result, decimal minRange)
        {
            var meanAverage = Math.Ceiling(SquareRoot(result));

            for (var i = minRange; i <= meanAverage; i++)
            {
                if (result % i != 0)
                {
                    continue;
                }

                var number = result / i;

                yield return (i, number);

                if (i != number)
                {
                    yield return (number, i);
                }
            }
        }

        public static IEnumerable<Tuple<decimal, decimal>> DivisorOf(decimal result, decimal maxRange)
        {
            for (var i = maxRange; i >= result; i--)
            {
                if (i % result != 0)
                {
                    continue;
                }

                yield return new Tuple<decimal, decimal>(i, i / result);
            }
        }

        private static unsafe int GetRandomBetween(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumber, int minimumValue, int maximumValue)
        {
            rngCryptoServiceProvider.GetBytes(randomNumber);

            fixed (byte* randomNumberPtr = randomNumber)
            {
                return (int) (minimumValue + MathF.Floor(MathF.Max(0, *randomNumberPtr / 255f - 0.0000001f) * (maximumValue - minimumValue + 1)));
            }
        }

        private static IEnumerable<decimal> DivideEvenly(decimal totalPossibilities, int totalThread)
        {
            var div = Math.Truncate(totalPossibilities / totalThread);
            var remainder = totalPossibilities % totalThread;

            for (var i = 0; i < totalThread; i++)
            {
                yield return i < remainder ? div + 1 : div;
            }
        }

        private static decimal SquareRoot(decimal square)
        {
            if (square < 0)
            {
                return 0;
            }

            var root = square / 3;

            for (var i = 0; i < 32; i++)
            {
                root = (root + square / root) / 2;
            }

            return root;
        }
    }
}