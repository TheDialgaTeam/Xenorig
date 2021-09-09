using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Xirorig.Utility
{
    internal static class Sha3Utility
    {
        private static class Native
        {
            [DllImport("xirorig_native")]
            public static extern int Sha3Utility_TryComputeSha512Hash(in byte source, int sourceLength, ref byte output, out int bytesWritten);
        }

        private class KeccakDigest
        {
            private static readonly ulong[] KeccakRoundConstants =
            {
                0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808aUL, 0x8000000080008000UL,
                0x000000000000808bUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
                0x000000000000008aUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000aUL,
                0x000000008000808bUL, 0x800000000000008bUL, 0x8000000000008089UL, 0x8000000000008003UL,
                0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800aUL, 0x800000008000000aUL,
                0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL
            };

            protected internal int fixedOutputLength;
            protected readonly byte[] dataQueue = new byte[192];
            protected int rate;
            protected int bitsInQueue;
            protected bool squeezing;

            private readonly ulong[] state = new ulong[25];

            public virtual string AlgorithmName => "Keccak-" + fixedOutputLength;

            public KeccakDigest() : this(288)
            {
            }

            public KeccakDigest(int bitLength)
            {
                Init(bitLength);
            }

            public KeccakDigest(KeccakDigest source)
            {
                CopyIn(source);
            }

            public virtual int GetDigestSize()
            {
                return fixedOutputLength >> 3;
            }

            public virtual void Update(byte input)
            {
                Absorb(input);
            }

            public virtual void BlockUpdate(ReadOnlySpan<byte> input)
            {
                Absorb(input);
            }

            public virtual int DoFinal(Span<byte> output)
            {
                Squeeze(output, fixedOutputLength);

                Reset();

                return GetDigestSize();
            }

            public virtual void Reset()
            {
                Init(fixedOutputLength);
            }

            /**
             * Return the size of block that the compression function is applied to in bytes.
             *
             * @return internal byte length of a block.
             */
            public virtual int GetByteLength()
            {
                return rate >> 3;
            }

            protected virtual int DoFinal(byte[] output, int outOff, byte partialByte, int partialBits)
            {
                if (partialBits > 0)
                {
                    AbsorbBits(partialByte, partialBits);
                }

                Squeeze(output.AsSpan(outOff), fixedOutputLength);

                Reset();

                return GetDigestSize();
            }

            protected void Absorb(byte data)
            {
                if ((bitsInQueue & 7) != 0)
                {
                    throw new InvalidOperationException("attempt to absorb with odd length queue");
                }

                if (squeezing)
                {
                    throw new InvalidOperationException("attempt to absorb while squeezing");
                }

                dataQueue[bitsInQueue >> 3] = data;

                if ((bitsInQueue += 8) == rate)
                {
                    KeccakAbsorb(dataQueue);
                    bitsInQueue = 0;
                }
            }

            protected void Absorb(ReadOnlySpan<byte> data)
            {
                if ((bitsInQueue & 7) != 0)
                {
                    throw new InvalidOperationException("attempt to absorb with odd length queue");
                }

                if (squeezing)
                {
                    throw new InvalidOperationException("attempt to absorb while squeezing");
                }

                var bytesInQueue = bitsInQueue >> 3;
                var rateBytes = rate >> 3;

                var available = rateBytes - bytesInQueue;

                if (data.Length < available)
                {
                    data.CopyTo(dataQueue.AsSpan(bytesInQueue, data.Length));
                    bitsInQueue += data.Length << 3;
                    return;
                }

                var count = 0;

                if (bytesInQueue > 0)
                {
                    data.CopyTo(dataQueue.AsSpan(bytesInQueue, available));
                    count += available;
                    KeccakAbsorb(dataQueue);
                }

                int remaining;

                while ((remaining = data.Length - count) >= rateBytes)
                {
                    KeccakAbsorb(data[count..]);
                    count += rateBytes;
                }

                data[count..].CopyTo(dataQueue.AsSpan(0, remaining));
                bitsInQueue = remaining << 3;
            }

            protected void AbsorbBits(int data, int bits)
            {
                if (bits < 1 || bits > 7)
                {
                    throw new ArgumentException("must be in the range 1 to 7", "bits");
                }

                if ((bitsInQueue & 7) != 0)
                {
                    throw new InvalidOperationException("attempt to absorb with odd length queue");
                }

                if (squeezing)
                {
                    throw new InvalidOperationException("attempt to absorb while squeezing");
                }

                var mask = (1 << bits) - 1;
                dataQueue[bitsInQueue >> 3] = (byte) (data & mask);

                // NOTE: After this, bitsInQueue is no longer a multiple of 8, so no more absorbs will work
                bitsInQueue += bits;
            }

            protected void Squeeze(Span<byte> output, long outputLength)
            {
                if (!squeezing)
                {
                    PadAndSwitchToSqueezingPhase();
                }

                if ((outputLength & 7L) != 0L)
                {
                    throw new InvalidOperationException("outputLength not a multiple of 8");
                }

                long i = 0;

                while (i < outputLength)
                {
                    if (bitsInQueue == 0)
                    {
                        KeccakExtract();
                    }

                    var partialBlock = (int) Math.Min(bitsInQueue, outputLength - i);
                    dataQueue.AsSpan((rate - bitsInQueue) >> 3, partialBlock >> 3).CopyTo(output[(int) (i >> 3)..]);
                    bitsInQueue -= partialBlock;
                    i += partialBlock;
                }
            }

            private void CopyIn(KeccakDigest source)
            {
                Array.Copy(source.state, 0, state, 0, source.state.Length);
                Array.Copy(source.dataQueue, 0, dataQueue, 0, source.dataQueue.Length);
                rate = source.rate;
                bitsInQueue = source.bitsInQueue;
                fixedOutputLength = source.fixedOutputLength;
                squeezing = source.squeezing;
            }

            private void Init(int bitLength)
            {
                switch (bitLength)
                {
                    case 128:
                    case 224:
                    case 256:
                    case 288:
                    case 384:
                    case 512:
                        InitSponge(1600 - (bitLength << 1));
                        break;

                    default:
                        throw new ArgumentException("must be one of 128, 224, 256, 288, 384, or 512.", "bitLength");
                }
            }

            private void InitSponge(int rate)
            {
                if (rate <= 0 || rate >= 1600 || (rate & 63) != 0)
                {
                    throw new InvalidOperationException("invalid rate value");
                }

                this.rate = rate;
                Array.Clear(state, 0, state.Length);
                Array.Fill(dataQueue, (byte) 0);
                bitsInQueue = 0;
                squeezing = false;
                fixedOutputLength = (1600 - rate) >> 1;
            }

            private void PadAndSwitchToSqueezingPhase()
            {
                dataQueue[bitsInQueue >> 3] |= (byte) (1 << (bitsInQueue & 7));

                if (++bitsInQueue == rate)
                {
                    KeccakAbsorb(dataQueue);
                }
                else
                {
                    int full = bitsInQueue >> 6, partial = bitsInQueue & 63;
                    var off = 0;

                    for (var i = 0; i < full; ++i)
                    {
                        state[i] ^= Unsafe.ReadUnaligned<ulong>(ref dataQueue[off]);
                        off += 8;
                    }

                    if (partial > 0)
                    {
                        var mask = (1UL << partial) - 1UL;
                        state[full] ^= Unsafe.ReadUnaligned<ulong>(ref dataQueue[off]) & mask;
                    }
                }

                state[(rate - 1) >> 6] ^= 1UL << 63;

                bitsInQueue = 0;
                squeezing = true;
            }

            private void KeccakAbsorb(ReadOnlySpan<byte> data)
            {
                var count = rate >> 6;

                for (var i = 0; i < count; ++i)
                {
                    state[i] ^= Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(data[(i * 8)..]));
                }

                KeccakPermutation();
            }

            private void KeccakExtract()
            {
                KeccakPermutation();

                var length = rate >> 6;

                for (var i = 0; i < length; i++)
                {
                    Unsafe.As<byte, ulong>(ref dataQueue[i * 8]) = state[i];
                }

                bitsInQueue = rate;
            }

            private void KeccakPermutation()
            {
                ulong[] A = state;

                ulong a00 = A[0], a01 = A[1], a02 = A[2], a03 = A[3], a04 = A[4];
                ulong a05 = A[5], a06 = A[6], a07 = A[7], a08 = A[8], a09 = A[9];
                ulong a10 = A[10], a11 = A[11], a12 = A[12], a13 = A[13], a14 = A[14];
                ulong a15 = A[15], a16 = A[16], a17 = A[17], a18 = A[18], a19 = A[19];
                ulong a20 = A[20], a21 = A[21], a22 = A[22], a23 = A[23], a24 = A[24];

                for (var i = 0; i < 24; i++)
                {
                    // theta
                    var c0 = a00 ^ a05 ^ a10 ^ a15 ^ a20;
                    var c1 = a01 ^ a06 ^ a11 ^ a16 ^ a21;
                    var c2 = a02 ^ a07 ^ a12 ^ a17 ^ a22;
                    var c3 = a03 ^ a08 ^ a13 ^ a18 ^ a23;
                    var c4 = a04 ^ a09 ^ a14 ^ a19 ^ a24;

                    var d1 = ((c1 << 1) | (c1 >> -1)) ^ c4;
                    var d2 = ((c2 << 1) | (c2 >> -1)) ^ c0;
                    var d3 = ((c3 << 1) | (c3 >> -1)) ^ c1;
                    var d4 = ((c4 << 1) | (c4 >> -1)) ^ c2;
                    var d0 = ((c0 << 1) | (c0 >> -1)) ^ c3;

                    a00 ^= d1;
                    a05 ^= d1;
                    a10 ^= d1;
                    a15 ^= d1;
                    a20 ^= d1;
                    a01 ^= d2;
                    a06 ^= d2;
                    a11 ^= d2;
                    a16 ^= d2;
                    a21 ^= d2;
                    a02 ^= d3;
                    a07 ^= d3;
                    a12 ^= d3;
                    a17 ^= d3;
                    a22 ^= d3;
                    a03 ^= d4;
                    a08 ^= d4;
                    a13 ^= d4;
                    a18 ^= d4;
                    a23 ^= d4;
                    a04 ^= d0;
                    a09 ^= d0;
                    a14 ^= d0;
                    a19 ^= d0;
                    a24 ^= d0;

                    // rho/pi
                    c1 = (a01 << 1) | (a01 >> 63);
                    a01 = (a06 << 44) | (a06 >> 20);
                    a06 = (a09 << 20) | (a09 >> 44);
                    a09 = (a22 << 61) | (a22 >> 3);
                    a22 = (a14 << 39) | (a14 >> 25);
                    a14 = (a20 << 18) | (a20 >> 46);
                    a20 = (a02 << 62) | (a02 >> 2);
                    a02 = (a12 << 43) | (a12 >> 21);
                    a12 = (a13 << 25) | (a13 >> 39);
                    a13 = (a19 << 8) | (a19 >> 56);
                    a19 = (a23 << 56) | (a23 >> 8);
                    a23 = (a15 << 41) | (a15 >> 23);
                    a15 = (a04 << 27) | (a04 >> 37);
                    a04 = (a24 << 14) | (a24 >> 50);
                    a24 = (a21 << 2) | (a21 >> 62);
                    a21 = (a08 << 55) | (a08 >> 9);
                    a08 = (a16 << 45) | (a16 >> 19);
                    a16 = (a05 << 36) | (a05 >> 28);
                    a05 = (a03 << 28) | (a03 >> 36);
                    a03 = (a18 << 21) | (a18 >> 43);
                    a18 = (a17 << 15) | (a17 >> 49);
                    a17 = (a11 << 10) | (a11 >> 54);
                    a11 = (a07 << 6) | (a07 >> 58);
                    a07 = (a10 << 3) | (a10 >> 61);
                    a10 = c1;

                    // chi
                    c0 = a00 ^ (~a01 & a02);
                    c1 = a01 ^ (~a02 & a03);
                    a02 ^= ~a03 & a04;
                    a03 ^= ~a04 & a00;
                    a04 ^= ~a00 & a01;
                    a00 = c0;
                    a01 = c1;

                    c0 = a05 ^ (~a06 & a07);
                    c1 = a06 ^ (~a07 & a08);
                    a07 ^= ~a08 & a09;
                    a08 ^= ~a09 & a05;
                    a09 ^= ~a05 & a06;
                    a05 = c0;
                    a06 = c1;

                    c0 = a10 ^ (~a11 & a12);
                    c1 = a11 ^ (~a12 & a13);
                    a12 ^= ~a13 & a14;
                    a13 ^= ~a14 & a10;
                    a14 ^= ~a10 & a11;
                    a10 = c0;
                    a11 = c1;

                    c0 = a15 ^ (~a16 & a17);
                    c1 = a16 ^ (~a17 & a18);
                    a17 ^= ~a18 & a19;
                    a18 ^= ~a19 & a15;
                    a19 ^= ~a15 & a16;
                    a15 = c0;
                    a16 = c1;

                    c0 = a20 ^ (~a21 & a22);
                    c1 = a21 ^ (~a22 & a23);
                    a22 ^= ~a23 & a24;
                    a23 ^= ~a24 & a20;
                    a24 ^= ~a20 & a21;
                    a20 = c0;
                    a21 = c1;

                    // iota
                    a00 ^= KeccakRoundConstants[i];
                }

                A[0] = a00;
                A[1] = a01;
                A[2] = a02;
                A[3] = a03;
                A[4] = a04;
                A[5] = a05;
                A[6] = a06;
                A[7] = a07;
                A[8] = a08;
                A[9] = a09;
                A[10] = a10;
                A[11] = a11;
                A[12] = a12;
                A[13] = a13;
                A[14] = a14;
                A[15] = a15;
                A[16] = a16;
                A[17] = a17;
                A[18] = a18;
                A[19] = a19;
                A[20] = a20;
                A[21] = a21;
                A[22] = a22;
                A[23] = a23;
                A[24] = a24;
            }
        }

        private class Sha3Digest : KeccakDigest
        {
            public override string AlgorithmName => "SHA3-" + fixedOutputLength;

            public Sha3Digest() : this(256)
            {
            }

            public Sha3Digest(int bitLength) : base(CheckBitLength(bitLength))
            {
            }

            public Sha3Digest(Sha3Digest source) : base(source)
            {
            }

            private static int CheckBitLength(int bitLength)
            {
                switch (bitLength)
                {
                    case 224:
                    case 256:
                    case 384:
                    case 512:
                        return bitLength;

                    default:
                        throw new ArgumentException(bitLength + " not supported for SHA-3", "bitLength");
                }
            }

            public override int DoFinal(Span<byte> output)
            {
                AbsorbBits(0x02, 2);

                return base.DoFinal(output);
            }

            protected override int DoFinal(byte[] output, int outOff, byte partialByte, int partialBits)
            {
                if (partialBits < 0 || partialBits > 7)
                {
                    throw new ArgumentException("must be in the range [0,7]", "partialBits");
                }

                var finalInput = (partialByte & ((1 << partialBits) - 1)) | (0x02 << partialBits);
                var finalBits = partialBits + 2;

                if (finalBits >= 8)
                {
                    Absorb((byte) finalInput);
                    finalBits -= 8;
                    finalInput >>= 8;
                }

                return base.DoFinal(output, outOff, (byte) finalInput, finalBits);
            }
        }

        public const int Sha512OutputSize = 512 / 8;

        private static bool _isNativeImplementationAvailable = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha512Hash(byte[] source)
        {
            return ComputeSha512Hash(source.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ComputeSha512Hash(byte[] source, int offset, int length)
        {
            return ComputeSha512Hash(source.AsSpan(offset, length));
        }

        public static byte[] ComputeSha512Hash(ReadOnlySpan<byte> source)
        {
            if (!_isNativeImplementationAvailable) return SoftwareComputeSha512Hash(source);

            try
            {
                var result = new byte[Sha512OutputSize];

                if (Native.Sha3Utility_TryComputeSha512Hash(Unsafe.AsRef(source[0]), source.Length, ref Unsafe.AsRef(result[0]), out var _) == 0) throw new CryptographicException();

                return result;
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                return SoftwareComputeSha512Hash(source);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryComputeSha512Hash(byte[] source, byte[] destination, out int bytesWritten)
        {
            return TryComputeSha512Hash(source.AsSpan(), destination.AsSpan(), out bytesWritten);
        }

        public static bool TryComputeSha512Hash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (!_isNativeImplementationAvailable) return SoftwareTryComputeSha512Hash(source, destination, out bytesWritten);

            try
            {
                return Native.Sha3Utility_TryComputeSha512Hash(MemoryMarshal.GetReference(source), source.Length, ref MemoryMarshal.GetReference(destination), out bytesWritten) == 1;
            }
            catch (Exception)
            {
                _isNativeImplementationAvailable = false;
                return SoftwareTryComputeSha512Hash(source, destination, out bytesWritten);
            }
        }

        private static byte[] SoftwareComputeSha512Hash(ReadOnlySpan<byte> source)
        {
            try
            {
                var result = new byte[Sha512OutputSize];

                var sha3Digest = new Sha3Digest(512);
                sha3Digest.BlockUpdate(source);
                sha3Digest.DoFinal(result);

                return result;
            }
            catch (Exception)
            {
                throw new CryptographicException();
            }
        }

        private static bool SoftwareTryComputeSha512Hash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            try
            {
                var sha3Digest = new Sha3Digest(512);
                sha3Digest.BlockUpdate(source);
                sha3Digest.DoFinal(destination);

                bytesWritten = Sha512OutputSize;
                return true;
            }
            catch (Exception)
            {
                bytesWritten = 0;
                return false;
            }
        }
    }
}