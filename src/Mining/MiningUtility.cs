using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining
{
    public static class MiningUtility
    {
        public static char[] RandomOperatorCalculation { get; } = { '+', '-', '*', '/', '%' };

        private static char[] Base10CharRepresentation { get; } = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private static char[] Base16CharRepresentation { get; } = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static RNGCryptoServiceProvider RngCryptoServiceProvider { get; } = new RNGCryptoServiceProvider();

        public static unsafe string ConvertStringToHexAndEncryptXorShare(string value, string key)
        {
            var valueLength = value.Length;
            var result = new string('\0', valueLength * 2);

            fixed (char* valuePtr = value, keyPtr = key, base16CharRepresentationPtr = Base16CharRepresentation, resultPtr = result)
            {
                var valueCharPtr = valuePtr;
                var resultCharPtr = resultPtr;

                var keyLength = key.Length;
                var keyIndex = 0;

                for (var i = 0; i < valueLength; i++)
                {
                    *resultCharPtr = (char) (*(base16CharRepresentationPtr + (*valueCharPtr >> 4)) ^ *(keyPtr + keyIndex));
                    resultCharPtr++;
                    keyIndex++;

                    if (keyIndex == keyLength)
                        keyIndex = 0;

                    *resultCharPtr = (char) (*(base16CharRepresentationPtr + (*valueCharPtr & 15)) ^ *(keyPtr + keyIndex));
                    resultCharPtr++;
                    keyIndex++;

                    if (keyIndex == keyLength)
                        keyIndex = 0;

                    valueCharPtr++;
                }
            }

            return result;
        }

        public static unsafe string EncryptXorShare(string value, string key)
        {
            var valueLength = value.Length;
            var result = new string('\0', valueLength);

            fixed (char* valuePtr = value, keyPtr = key, resultPtr = result)
            {
                var valueCharPtr = valuePtr;
                var resultCharPtr = resultPtr;

                var keyLength = key.Length;
                var keyIndex = 0;

                for (var i = 0; i < valueLength; i++)
                {
                    *resultCharPtr = (char) (*valueCharPtr ^ *(keyPtr + keyIndex));
                    resultCharPtr++;
                    keyIndex++;

                    if (keyIndex == keyLength)
                        keyIndex = 0;

                    valueCharPtr++;
                }
            }

            return result;
        }

        public static unsafe string EncryptAesShare(ICryptoTransform aesCryptoTransform, string value)
        {
            var textBytes = Encoding.UTF8.GetBytes(value);
            var output = aesCryptoTransform.TransformFinalBlock(textBytes, 0, textBytes.Length);
            var outputLength = output.Length;
            var result = new string('\0', outputLength * 2 + outputLength - 1);

            fixed (byte* outputPtr = output)
            fixed (char* base16CharRepresentationPtr = Base16CharRepresentation, resultPtr = result)
            {
                var outputBytePtr = outputPtr;
                var resultCharPtr = resultPtr;

                for (var i = 0; i < outputLength; i++)
                {
                    *resultCharPtr = *(base16CharRepresentationPtr + (*outputBytePtr >> 4));
                    resultCharPtr++;

                    *resultCharPtr = *(base16CharRepresentationPtr + (*outputBytePtr & 15));
                    resultCharPtr++;

                    if (i == outputLength - 1)
                        break;

                    *resultCharPtr = '-';
                    resultCharPtr++;

                    outputBytePtr++;
                }
            }

            return result;
        }

        public static unsafe string EncryptAesShareRoundAndEncryptXorShare(ICryptoTransform aesCryptoTransform, string value, int round, string key)
        {
            var result = value;
            var keyLength = key.Length;

            for (var i = 0; i < round; i++)
            {
                var textBytes = Encoding.UTF8.GetBytes(result);
                var output = aesCryptoTransform.TransformFinalBlock(textBytes, 0, textBytes.Length);
                var outputLength = output.Length;

                result = new string('\0', outputLength * 2 + outputLength - 1);

                fixed (byte* outputPtr = output)
                fixed (char* keyPtr = key, base16CharRepresentationPtr = Base16CharRepresentation, resultPtr = result)
                {
                    var outputBytePtr = outputPtr;
                    var resultCharPtr = resultPtr;
                    var keyIndex = 0;

                    for (var j = 0; j < outputLength; j++)
                    {
                        if (i >= round - 1)
                        {
                            *resultCharPtr = (char) (*(base16CharRepresentationPtr + (*outputBytePtr >> 4)) ^ *(keyPtr + keyIndex));
                            resultCharPtr++;
                            keyIndex++;

                            if (keyIndex == keyLength)
                                keyIndex = 0;

                            *resultCharPtr = (char) (*(base16CharRepresentationPtr + (*outputBytePtr & 15)) ^ *(keyPtr + keyIndex));
                            resultCharPtr++;
                            keyIndex++;

                            if (keyIndex == keyLength)
                                keyIndex = 0;

                            if (j == outputLength - 1)
                                break;

                            *resultCharPtr = (char) ('-' ^ *(keyPtr + keyIndex));
                            resultCharPtr++;
                            keyIndex++;

                            if (keyIndex == keyLength)
                                keyIndex = 0;
                        }
                        else
                        {
                            *resultCharPtr = *(base16CharRepresentationPtr + (*outputBytePtr >> 4));
                            resultCharPtr++;

                            *resultCharPtr = *(base16CharRepresentationPtr + (*outputBytePtr & 15));
                            resultCharPtr++;

                            if (j == outputLength - 1)
                                break;

                            *resultCharPtr = '-';
                            resultCharPtr++;
                        }

                        outputBytePtr++;
                    }
                }
            }

            return result;
        }

        public static unsafe string ComputeHash(HashAlgorithm hashAlgorithm, string value)
        {
            var output = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            var outputLength = output.Length;
            var result = new string('\0', outputLength * 2);

            fixed (byte* outputPtr = output)
            fixed (char* base16CharRepresentationPtr = Base16CharRepresentation, resultPtr = result)
            {
                var outputBytePtr = outputPtr;
                var resultCharPtr = resultPtr;

                for (var i = 0; i < outputLength; i++)
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

        public static unsafe string HashJobToHexString(string value)
        {
            var output = Encoding.Unicode.GetBytes(value);
            var outputLength = output.Length;
            var result = new string('\0', outputLength * 2);

            fixed (byte* outputPtr = output)
            fixed (char* base16CharRepresentationPtr = Base16CharRepresentation, resultPtr = result)
            {
                var outputBytePtr = outputPtr;
                var resultCharPtr = resultPtr;

                for (var i = 0; i < outputLength; i++)
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

        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {
            var randomNumber = new byte[1];
            RngCryptoServiceProvider.GetBytes(randomNumber);
            return (int) (minimumValue + Math.Floor(Math.Max(0, randomNumber[0] / 255d - 0.00000000001d) * (maximumValue - minimumValue + 1)));
        }

        public static decimal GetRandomBetweenJob(decimal minimumValue, decimal maximumValue)
        {
            var randomNumber = new byte[1];
            RngCryptoServiceProvider.GetBytes(randomNumber);
            return minimumValue + Math.Floor(Math.Max(0, randomNumber[0] / 255m - 0.00000000001m) * (maximumValue - minimumValue + 1));
        }

        public static unsafe decimal GenerateNumberMathCalculation(decimal minRange, decimal maxRange)
        {
            decimal resultDecimal;

            do
            {
                var randomSize = GetRandomBetween(1, GetRandomBetweenJob(minRange, maxRange).ToString("F0").Length);
                var resultString = new string('\0', randomSize);

                fixed (char* base10CharRepresentationPtr = Base10CharRepresentation, resultPtr = resultString)
                {
                    var resultCharPtr = resultPtr;

                    for (var i = 0; i < randomSize; i++)
                    {
                        if (randomSize == 1)
                            *resultCharPtr = *(base10CharRepresentationPtr + GetRandomBetween(2, 9));
                        else
                            *resultCharPtr = *(base10CharRepresentationPtr + GetRandomBetween(i == 0 ? 1 : 0, 9));

                        resultCharPtr++;
                    }
                }

                resultDecimal = Convert.ToDecimal(resultString);
            } while (resultDecimal < minRange || resultDecimal > maxRange);

            return resultDecimal;
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
                return false;

            if (number == 2)
                return true;

            if (number % 2 == 0)
                return false;

            var boundary = Math.Floor(SquareRoot(number));

            for (var i = 3m; i <= boundary; i += 2)
            {
                if (number % i == 0)
                    return false;
            }

            return true;
        }

        public static IEnumerable<(decimal, decimal)> SumOf(decimal result, decimal minRange)
        {
            if (result == 2 || result == 3)
                yield break;

            for (var i = minRange; i < result - 1; i++)
            {
                var number = result - i;

                yield return (i, result - i);

                if (i != number)
                    yield return (result - i, i);
            }
        }

        public static IEnumerable<Tuple<decimal, decimal>> SubtractOf(decimal result, decimal maxRange)
        {
            for (var i = maxRange; i > result + 1; i--)
                yield return new Tuple<decimal, decimal>(i, i - result);
        }

        public static IEnumerable<(decimal, decimal)> FactorOf(decimal result, decimal minRange)
        {
            var meanAverage = Math.Ceiling(SquareRoot(result));

            for (var i = minRange; i <= meanAverage; i++)
            {
                if (result % i != 0)
                    continue;

                var number = result / i;

                yield return (i, number);

                if (i != number)
                    yield return (number, i);
            }
        }

        public static IEnumerable<Tuple<decimal, decimal>> DivisorOf(decimal result, decimal maxRange)
        {
            for (var i = maxRange; i >= result; i--)
            {
                if (i % result != 0)
                    continue;

                yield return new Tuple<decimal, decimal>(i, i / result);
            }
        }

        private static IEnumerable<decimal> DivideEvenly(decimal totalPossibilities, int totalThread)
        {
            var div = Math.Truncate(totalPossibilities / totalThread);
            var remainder = totalPossibilities % totalThread;

            for (var i = 0; i < totalThread; i++)
                yield return i < remainder ? div + 1 : div;
        }

        private static decimal SquareRoot(decimal square)
        {
            if (square < 0)
                return 0;

            var root = square / 3;

            for (var i = 0; i < 32; i++)
                root = (root + square / root) / 2;

            return root;
        }
    }
}