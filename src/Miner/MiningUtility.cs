using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner
{
    public static class MiningUtility
    {
        private const int MaxFloatPrecision = 16777216;
        private const long MaxDoublePrecision = 9007199254740992;

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

        public static int GenerateNumberMathCalculation(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumberBytes, int minRange, int maxRange, int minSize, int maxSize)
        {
            do
            {
                var randomSize = GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, minSize, maxSize);

                if (randomSize == 1)
                {
                    return GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, minRange, 9);
                }

                var result = 0;
                var digit = 1;

                if (randomSize < 10)
                {
                    for (var i = randomSize - 1; i >= 0; i--)
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, i == 0 ? 1 : 0, 9);
                        digit *= 10;
                    }
                }
                else
                {
                    for (var i = randomSize - 2; i >= 0; i--)
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, 0, 9);
                        digit *= 10;
                    }

                    if (result <= 147483647)
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, 1, 2);
                    }
                    else
                    {
                        result += digit * 1;
                    }
                }

                if (result > minRange && result < maxRange) return result;
            } while (true);
        }

        public static long GenerateNumberMathCalculation(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumberBytes, long minRange, long maxRange, int minSize, int maxSize)
        {
            do
            {
                var randomSize = GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, minSize, maxSize);

                if (randomSize == 1)
                {
                    return GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, minRange, 9);
                }

                var result = 0L;
                var digit = 1L;

                if (randomSize < 19)
                {
                    for (var i = randomSize - 1; i >= 0; i--)
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, i == 0 ? 1 : 0, 9);
                        digit *= 10;
                    }
                }
                else
                {
                    for (var i = randomSize - 2; i >= 0; i--)
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, 0, 9);
                        digit *= 10;
                    }

                    if (result <= 223372036854775807)
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, 1, 9);
                    }
                    else
                    {
                        result += digit * GetRandomBetween(rngCryptoServiceProvider, randomNumberBytes, 1, 8);
                    }
                }

                if (result > minRange && result < maxRange) return result;
            } while (true);
        }

        public static unsafe int GetRandomBetween(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumberBytes, int minimumValue, int maximumValue)
        {
            rngCryptoServiceProvider.GetBytes(randomNumberBytes);

            fixed (byte* randomNumberPtr = randomNumberBytes)
            {
                var factor = maximumValue - minimumValue + 1;

                if (factor <= MaxFloatPrecision)
                {
                    return minimumValue + (int) (MathF.Max(0, *randomNumberPtr / 255f - 0.0000001f) * factor);
                }

                return minimumValue + (int) (Math.Max(0, *randomNumberPtr / 255d - 0.00000000001) * factor);
            }
        }

        public static unsafe long GetRandomBetween(RNGCryptoServiceProvider rngCryptoServiceProvider, byte[] randomNumberBytes, long minimumValue, long maximumValue)
        {
            rngCryptoServiceProvider.GetBytes(randomNumberBytes);

            fixed (byte* randomNumberPtr = randomNumberBytes)
            {
                var factor = maximumValue - minimumValue + 1;

                if (factor <= MaxFloatPrecision)
                {
                    return minimumValue + (long) (MathF.Max(0, *randomNumberPtr / 255f - 0.0000001f) * factor);
                }

                return minimumValue + (long) (Math.Max(0, *randomNumberPtr / 255d - 0.00000000001) * factor);
            }
        }
    }
}