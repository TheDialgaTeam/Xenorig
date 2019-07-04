﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining
{
    public static class MiningUtility
    {
        public static string[] RandomOperatorCalculation { get; } = { "+", "-", "*", "/", "%" };

        private static char[] Base10CharRepresentation { get; } = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private static char[] Base16CharRepresentation { get; } = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static SHA512 Sha512 { get; } = SHA512.Create();

        private static RNGCryptoServiceProvider RngCryptoServiceProvider { get; } = new RNGCryptoServiceProvider();

        public static unsafe string ConvertStringToHexAndEncryptXorShare(string value, string key)
        {
            var base16CharRepresentation = Base16CharRepresentation;
            var valueLength = value.Length;
            var keyLength = key.Length;
            var result = new string('\0', valueLength * 2);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;
                var keyIndex = 0;

                for (var i = 0; i < valueLength; i++)
                {
                    *charPtr = (char) (base16CharRepresentation[value[i] >> 4] ^ key[keyIndex]);
                    charPtr++;
                    keyIndex++;

                    if (keyIndex >= keyLength)
                        keyIndex = 0;

                    *charPtr = (char) (base16CharRepresentation[value[i] & 15] ^ key[keyIndex]);
                    charPtr++;
                    keyIndex++;

                    if (keyIndex >= keyLength)
                        keyIndex = 0;
                }
            }

            return result;
        }

        public static unsafe string EncryptXorShare(string value, string key)
        {
            var valueLength = value.Length;
            var keyLength = key.Length;
            var result = new string('\0', valueLength);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;
                var keyIndex = 0;

                for (var i = 0; i < valueLength; i++)
                {
                    *charPtr = (char) (value[i] ^ key[keyIndex]);
                    charPtr++;
                    keyIndex++;

                    if (keyIndex >= keyLength)
                        keyIndex = 0;
                }
            }

            return result;
        }

        public static string EncryptAesShare(ICryptoTransform aesCryptoTransform, string text)
        {
            var textBytes = Encoding.UTF8.GetBytes(text);
            var result = aesCryptoTransform.TransformFinalBlock(textBytes, 0, textBytes.Length);

            return BitConverter.ToString(result);
        }

        public static string EncryptAesShareRound(ICryptoTransform aesCryptoTransform, string text, int round)
        {
            var textToEncrypt = text;

            for (var i = 0; i < round; i++)
            {
                var textBytes = Encoding.UTF8.GetBytes(textToEncrypt);
                var result = aesCryptoTransform.TransformFinalBlock(textBytes, 0, textBytes.Length);
                textToEncrypt = BitConverter.ToString(result);
            }

            return textToEncrypt;
        }

        public static unsafe string GenerateSha512(string value)
        {
#if NETCOREAPP
            var base16CharRepresentation = Base16CharRepresentation;
            var hashedInputBytes = Sha512.ComputeHash(Encoding.UTF8.GetBytes(value));
            var result = new string('\0', 128);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;

                for (var i = 0; i < 64; i++)
                {
                    *charPtr = base16CharRepresentation[hashedInputBytes[i] >> 4];
                    charPtr++;

                    *charPtr = base16CharRepresentation[hashedInputBytes[i] & 15];
                    charPtr++;
                }
            }

            return result;
#else
            var base16CharRepresentation = Base16CharRepresentation;
            var hashedInputBytes = Sha512.ComputeHash(GetUtf8Bytes(value));
            var result = new string('\0', 128);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;

                for (var i = 0; i < 64; i++)
                {
                    *charPtr = base16CharRepresentation[hashedInputBytes[i] >> 4];
                    charPtr++;

                    *charPtr = base16CharRepresentation[hashedInputBytes[i] & 15];
                    charPtr++;
                }
            }

            return result;
#endif
        }

        public static unsafe string HashJobToHexString(string value)
        {
            var base16CharRepresentation = Base16CharRepresentation;
            var bytes = Encoding.Unicode.GetBytes(value);
            var result = new string('\0', 512);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;

                for (var i = 0; i < 256; i++)
                {
                    *charPtr = base16CharRepresentation[bytes[i] >> 4];
                    charPtr++;

                    *charPtr = base16CharRepresentation[bytes[i] & 15];
                    charPtr++;
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

        public static decimal GenerateNumberMathCalculation(decimal minRange, decimal maxRange)
        {
#if NETCOREAPP
            decimal resultDecimal;

            do
            {
                var randomSize = GetRandomBetween(1, GetRandomBetweenJob(minRange, maxRange).ToString("F0").Length);

                var resultString = string.Create(randomSize, randomSize, (result, state) =>
                {
                    var digitCharRepresentation = Base10CharRepresentation;

                    for (var i = 0; i < state; i++)
                    {
                        if (state == 1)
                            result[i] = digitCharRepresentation[GetRandomBetween(2, 9)];
                        else
                            result[i] = digitCharRepresentation[GetRandomBetween(i == 0 ? 1 : 0, 9)];
                    }
                });

                resultDecimal = Convert.ToDecimal(resultString);
            } while (resultDecimal < minRange || resultDecimal > maxRange);

            return resultDecimal;
#else
            decimal resultDecimal;
            var digitCharRepresentation = Base10CharRepresentation;

            do
            {
                var randomSize = GetRandomBetween(1, GetRandomBetweenJob(minRange, maxRange).ToString("F0").Length);
                var resultString = new char[randomSize];

                for (var i = 0; i < randomSize; i++)
                {
                    if (randomSize == 1)
                        resultString[i] = digitCharRepresentation[GetRandomBetween(2, 9)];
                    else
                        resultString[i] = digitCharRepresentation[GetRandomBetween(i == 0 ? 1 : 0, 9)];
                }
                    

                resultDecimal = Convert.ToDecimal(new string(resultString));
            } while (resultDecimal < minRange || resultDecimal > maxRange);

            return resultDecimal;
#endif
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

        private static byte[] GetUtf8Bytes(string value)
        {
            var valueLength = value.Length;
            var result = new byte[valueLength];

            for (var i = 0; i < valueLength; i++)
                result[i] = (byte) value[i];

            return result;
        }
    }
}