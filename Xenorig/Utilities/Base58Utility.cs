using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Xenorig.Utilities;

public static class Base58Utility
{
    private static readonly char[] Characters =
    {
        '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeLength(byte[] value)
    {
        return EncodeLength(value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeLength(byte[] value, int offset, int length)
    {
        return EncodeLength(value.AsSpan(offset, length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeLength(ReadOnlySpan<byte> value)
    {
        return (int) Math.Ceiling(value.Length * 8 / 6.0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeLength(string value)
    {
        return DecodeLength(value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeLength(ReadOnlySpan<char> value)
    {
        return (int) Math.Ceiling(value.Length * 6 / 8.0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Encode(byte[] value)
    {
        return Encode(value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Encode(byte[] value, int offset, int length)
    {
        return Encode(value.AsSpan(offset, length));
    }

    public static string Encode(ReadOnlySpan<byte> value)
    {
        var payloadValue = new BigInteger(value, true, true);
        var encodeLength = EncodeLength(value);

        return string.Create(encodeLength, (payloadValue, encodeLength, Characters), (span, state) =>
        {
            var (payloadValueState, encodeLengthState, charactersState) = state;
            var currentIndex = encodeLengthState - 1;

            while (payloadValueState > 0)
            {
                payloadValueState = BigInteger.DivRem(payloadValueState, 58, out var remainder);
                span[currentIndex--] = charactersState[(int) remainder];
            }

            var zeroCharacter = charactersState[0];

            for (; currentIndex >= 0; currentIndex--)
            {
                span[currentIndex] = zeroCharacter;
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryEncode(byte[] value, char[] output, out int charsWritten)
    {
        return TryEncode(value.AsSpan(), output.AsSpan(), out charsWritten);
    }

    public static bool TryEncode(ReadOnlySpan<byte> value, Span<char> output, out int charsWritten)
    {
        var encodeLength = EncodeLength(value);

        if (output.Length < encodeLength)
        {
            charsWritten = 0;
            return false;
        }

        var currentIndex = encodeLength - 1;
        var payloadValue = new BigInteger(value, true, true);

        while (payloadValue > 0)
        {
            payloadValue = BigInteger.DivRem(payloadValue, 58, out var remainder);
            output[currentIndex--] = Characters[(int) remainder];
        }

        var zeroCharacter = Characters[0];

        for (; currentIndex >= 0; currentIndex--)
        {
            output[currentIndex] = zeroCharacter;
        }

        charsWritten = encodeLength;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decode(string value)
    {
        return Decode(value.AsSpan());
    }

    public static byte[] Decode(ReadOnlySpan<char> value)
    {
        var decodeLength = DecodeLength(value);
        var output = new byte[decodeLength];
        var outputSpan = output.AsSpan();

        var payloadValue = BigInteger.Zero;

        foreach (var character in value)
        {
            payloadValue = payloadValue * 58 + Array.IndexOf(Characters, character);
        }

        payloadValue.TryWriteBytes(outputSpan, out var bytesWritten, true, true);

        if (bytesWritten == decodeLength) return output;

        outputSpan[..bytesWritten].CopyTo(outputSpan[(decodeLength - bytesWritten - 1)..]);
        outputSpan[..(decodeLength - bytesWritten)].Fill(0);

        return output;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecode(string value, byte[] output, out int bytesWritten)
    {
        return TryDecode(value.AsSpan(), output.AsSpan(), out bytesWritten);
    }

    public static bool TryDecode(ReadOnlySpan<char> value, Span<byte> output, out int bytesWritten)
    {
        var decodeLength = DecodeLength(value);

        if (output.Length < decodeLength)
        {
            bytesWritten = 0;
            return false;
        }

        var payloadValue = BigInteger.Zero;

        foreach (var character in value)
        {
            payloadValue = payloadValue * 58 + Array.IndexOf(Characters, character);
        }

        payloadValue.TryWriteBytes(output, out bytesWritten, true, true);

        if (bytesWritten == decodeLength) return true;

        output[..bytesWritten].CopyTo(output[(decodeLength - bytesWritten - 1)..]);
        output[..(decodeLength - bytesWritten)].Fill(0);

        bytesWritten = decodeLength;
        return true;
    }
}