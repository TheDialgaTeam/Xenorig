using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner;

public static class MiningUtility
{
    private const int MaxFloatPrecision = 16777216;
    private const long MaxDoublePrecision = 9007199254740992;

    private static readonly char[] Base16CharRepresentation = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

    private static readonly byte[] Base16ByteRepresentation = "0123456789ABCDEF"u8.ToArray();

    public static unsafe string MakeEncryptedShare(string value, string xorKey, int round, ICryptoTransform aesCryptoTransform)
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
            var hashOutput = sharedArrayPool.Rent(512 / 8);
            
            try
            {
                SHA512.TryHashData(output.AsSpan(0, outputLength), hashOutput, out var hashOutputLength);
                return Convert.ToHexString(hashOutput.AsSpan(0, hashOutputLength));
            }
            finally
            {
                sharedArrayPool.Return(output);
                sharedArrayPool.Return(hashOutput);
            }
        }
    }

    public static string ComputeHash(string value)
    {
        var output = ArrayPool<byte>.Shared.Rent(512 / 8);

        try
        {
            return !SHA512.TryHashData(Encoding.ASCII.GetBytes(value), output, out var outputLength) ? string.Empty : Convert.ToHexString(output.AsSpan(0, outputLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(output);
        }
    }
}