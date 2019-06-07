using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TheDialgaTeam.Xiropht.Xirorig
{
    public static class MiningUtility
    {
        public static string[] RandomOperatorCalculation = { "+", "*", "%", "-", "/" };

        private static string[] RandomNumberCalculation { get; } = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private static SHA512 Sha512 { get; } = SHA512.Create();

        private static RNGCryptoServiceProvider RngCryptoServiceProvider { get; } = new RNGCryptoServiceProvider();

        public static string StringToHexString(string hex)
        {
            var buffer = Encoding.UTF8.GetBytes(hex);
            return BitConverter.ToString(buffer).Replace("-", "");
        }

        public static string EncryptXorShare(string text, string key)
        {
            var result = new StringBuilder();

            for (var c = 0; c < text.Length; c++)
                result.Append((char) (text[c] ^ (uint) key[c % key.Length]));

            return result.ToString();
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

        public static string GenerateSha512(string input)
        {
            var hashedInputBytes = Sha512.ComputeHash(Encoding.UTF8.GetBytes(input));

            var hashedInputStringBuilder = new StringBuilder(128);

            foreach (var b in hashedInputBytes)
                hashedInputStringBuilder.Append(b.ToString("X2"));

            return hashedInputStringBuilder.ToString();
        }

        public static string HashJobToHexString(string str)
        {
            var sb = new StringBuilder();

            var bytes = Encoding.Unicode.GetBytes(str);

            foreach (var t in bytes)
                sb.Append(t.ToString("X2"));

            return sb.ToString();
        }

        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {
            var randomNumber = new byte[sizeof(int)];

            RngCryptoServiceProvider.GetBytes(randomNumber);

            var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);
            var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);
            var range = maximumValue - minimumValue + 1;
            var randomValueInRange = Math.Floor(multiplier * range);

            return (int)(minimumValue + randomValueInRange);
        }

        public static decimal GetRandomBetweenJob(decimal minimumValue, decimal maximumValue)
        {
            var randomNumber = new byte[sizeof(decimal)];

            RngCryptoServiceProvider.GetBytes(randomNumber);

            var asciiValueOfRandomCharacter = (decimal)Convert.ToDouble(randomNumber[0]);
            var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255m - 0.00000000001m);
            var range = maximumValue - minimumValue + 1;
            var randomValueInRange = Math.Floor(multiplier * range);

            return minimumValue + randomValueInRange;
        }

        public static string GenerateNumberMathCalculation(decimal minRange, decimal maxRange, int currentBlockDifficultyLength)
        {
            var number = "0";
            var numberBuilder = new StringBuilder();

            while (decimal.Parse(number) > maxRange || decimal.Parse(number) <= 1 || number.Length > currentBlockDifficultyLength)
            {
                var randomJobSize = GetRandomBetweenJob(minRange, maxRange).ToString("F0").Length;

                var randomSize = GetRandomBetween(1, randomJobSize);
                var counter = 0;

                while (counter < randomSize)
                {
                    if (randomSize > 1)
                    {
                        var numberRandom = RandomNumberCalculation[GetRandomBetween(0, RandomNumberCalculation.Length - 1)];

                        if (counter == 0)
                        {
                            while (numberRandom == "0")
                                numberRandom = RandomNumberCalculation[GetRandomBetween(0, RandomNumberCalculation.Length - 1)];
                            numberBuilder.Append(numberRandom);
                        }
                        else
                            numberBuilder.Append(numberRandom);
                    }
                    else
                        numberBuilder.Append(RandomNumberCalculation[GetRandomBetween(0, RandomNumberCalculation.Length - 1)]);

                    counter++;
                }

                number = numberBuilder.ToString();
                numberBuilder.Clear();

                return number;
            }

            return number;
        }

        public static (decimal, decimal) GetJobRange(decimal totalPossibilities, int totalThread, int threadIndex, decimal offset)
        {
            var startRange = DivideEvenly(totalPossibilities, totalThread).Take(threadIndex + 1).Sum() - DivideEvenly(totalPossibilities, totalThread).ElementAt(threadIndex) + offset;
            var endRange = DivideEvenly(totalPossibilities, totalThread).Take(threadIndex + 1).Sum() + offset - 1;

            return (startRange, endRange);
        }

        public static (decimal, decimal) GetJobRangeByPercentage(decimal minRange, decimal maxRange, int minRangePercentage, int maxRangePercentage)
        {
            var startRange = maxRange * minRangePercentage * 0.01m + minRange;
            var endRange = Math.Floor(maxRange * maxRangePercentage * 0.01m) + 1;

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