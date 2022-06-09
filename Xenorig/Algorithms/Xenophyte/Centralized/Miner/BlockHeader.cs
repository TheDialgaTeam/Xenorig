using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xenorig.Utilities.KeyDerivationFunction;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal partial class XenophyteCentralizedAlgorithm
{
    private static partial class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern int XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(long minValue, long maxValue, in long output);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(long minValue, long maxvalue, in long easyBlockValues, int easyBlockValuesLength, in long output);
    }

    private readonly ReaderWriterLockSlim _blockHeaderLock = new();

    private int _blockHeight;
    private long _blockTimestampCreate;

    private string _blockMethod = string.Empty;
    private string _blockIndication = string.Empty;

    private long _blockDifficulty;
    private long _blockMinRange;
    private long _blockMaxRange;

    private byte[] _blockAesPassword = Array.Empty<byte>();
    private int _blockAesPasswordLength;

    private byte[] _blockAesSalt = Array.Empty<byte>();
    private int _blockAesSaltLength;

    private readonly byte[] _blockAesIv = new byte[16];
    private byte[] _blockAesKey = Array.Empty<byte>();
    private int _blockAesKeySize;
    private int _blockAesRound;

    private byte[] _blockXorKey = Array.Empty<byte>();
    private int _blockXorKeyLength;

    private readonly long[] _easyBlockValues = new long[256];
    private int _easyBlockValuesLength;

    private long[] _nonEasyBlockValues = Array.Empty<long>();
    private long _nonEasyBlockValuesLength;

    private string _currentBlockIndication = string.Empty;
    private int _isBlockFound;

    private bool UpdateBlockHeader(ReadOnlySpan<byte> packet)
    {
        // SEND-CURRENT-BLOCK-MINING|
        // ID=1050878&
        // HASH=8D67ED6841B2305E0B23A5D009015F466BCBCEA0B5541263E0682B2E576B0CB9&
        // ALGORITHM=AES&
        // SIZE=256&
        // METHOD=XENOPHYTE&
        // KEY=Uos4dc09b3pgQGnII985eLmfLRKW6gG5FC3J6ctarAnRe2Uhof0a5Rx5vBi0JekuKz1ufEzJmp1ZouJC90sMFcr62WgGgidV5XLX12llxw4WuerTp4ZZdrOm82m075walihO54MEP8na2VYfRkJU344wzbbG0XjqtsSp3Tlh2UbJbYJr7KxjVlMVf9hRo344gG8xLRb98jpP5dkAnAAKBlJJZcqHgz6JqOamhOegwdUSurYP48wIZxbCKPTWeAtY&
        // JOB=2;10107&
        // REWARD=10.00000000&
        // DIFFICULTY=10107&
        // TIMESTAMP=1654187614&
        // INDICATION=ECBC42F0E83175E174FA6AB566EBEA88D97C2195BDB6E19483CDAD1FFC8EAF2A657BB8EF53E0D04FAFEF7539F7E9724F820517F560829288C0E04B4C58DD693B&
        // NETWORK_HASHRATE=12128&
        // LIFETIME=360

        try
        {
            _blockHeaderLock.EnterWriteLock();

            Span<char> packetString = stackalloc char[Encoding.ASCII.GetCharCount(packet)];
            Encoding.ASCII.GetChars(packet, packetString);

            if (!packetString[..SendCurrentBlockMining.Length].SequenceEqual(SendCurrentBlockMining)) return false;

            ReadOnlySpan<char> current = packetString[(SendCurrentBlockMining.Length + 1)..];

            while (current.Length > 0)
            {
                var keyIndex = current.IndexOf('=');
                if (keyIndex == -1) return false;

                var key = current[..keyIndex];

                current = current[(keyIndex + 1)..];

                var valueIndex = current.IndexOf('&');
                var value = current[..current.Length];

                if (valueIndex > 0)
                {
                    value = current[..valueIndex];
                    current = current[(valueIndex + 1)..];
                }
                else
                {
                    current = ReadOnlySpan<char>.Empty;
                }

                if (key.SequenceEqual("ID"))
                {
                    _blockHeight = int.Parse(value);
                }
                else if (key.SequenceEqual("METHOD"))
                {
                    if (value.SequenceEqual(_blockMethod)) continue;
                    _blockMethod = value.ToString();
                }
                else if (key.SequenceEqual("DIFFICULTY"))
                {
                    _blockDifficulty = long.Parse(value);
                }
                else if (key.SequenceEqual("TIMESTAMP"))
                {
                    _blockTimestampCreate = long.Parse(value);
                }
                else if (key.SequenceEqual("KEY"))
                {
                    var aesPasswordLength = Encoding.ASCII.GetByteCount(value);

                    if (_blockAesPassword.Length < aesPasswordLength)
                    {
                        _blockAesPassword = new byte[aesPasswordLength];
                    }

                    _blockAesPasswordLength = Encoding.ASCII.GetBytes(value, _blockAesPassword);
                }
                else if (key.SequenceEqual("INDICATION"))
                {
                    if (value.SequenceEqual(_blockIndication)) continue;
                    _blockIndication = value.ToString();
                }
                else if (key.SequenceEqual("JOB"))
                {
                    var index = value.IndexOf(';');
                    _blockMinRange = long.Parse(value[..index]);
                    _blockMaxRange = long.Parse(value[(index + 1)..]);
                }
            }

            _easyBlockValuesLength = Native.XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(_blockMinRange, _blockMaxRange, Unsafe.AsRef(_easyBlockValues[0]));
            _nonEasyBlockValuesLength = _blockMaxRange - _blockMinRange + 1 - _easyBlockValuesLength;

            if (_nonEasyBlockValuesLength > 0 && _nonEasyBlockValues.LongLength < _nonEasyBlockValuesLength)
            {
                _nonEasyBlockValues = new long[_nonEasyBlockValuesLength];
            }

            if (_nonEasyBlockValuesLength > 0)
            {
                Native.XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(_blockMinRange, _blockMaxRange, Unsafe.AsRef(_easyBlockValues[0]), _easyBlockValuesLength, Unsafe.AsRef(_nonEasyBlockValues[0]));
            }
        }
        finally
        {
            _blockHeaderLock.ExitWriteLock();
        }

        return true;
    }

    private bool UpdateBlockMethod(ReadOnlySpan<byte> packet)
    {
        // SEND-CONTENT-BLOCK-METHOD|
        // 1#128#128#128

        try
        {
            _blockHeaderLock.EnterWriteLock();

            Span<char> packetString = stackalloc char[Encoding.ASCII.GetCharCount(packet)];
            Encoding.ASCII.GetChars(packet, packetString);

            if (!packetString[..SendContentBlockMethod.Length].SequenceEqual(SendContentBlockMethod)) return false;

            ReadOnlySpan<char> current = packetString[(SendContentBlockMethod.Length + 1)..];

            var index = current.IndexOf('#');
            if (index < 0) return false;

            _blockAesRound = int.Parse(current[..index]);

            current = current[(index + 1)..];

            index = current.IndexOf('#');
            if (index < 0) return false;

            _blockAesKeySize = int.Parse(current[..index]);

            if (_blockAesKey.Length < _blockAesKeySize)
            {
                _blockAesKey = new byte[_blockAesKeySize];
            }

            current = current[(index + 1)..];

            index = current.IndexOf('#');
            if (index < 0) return false;

            var aesSaltLength = Encoding.ASCII.GetByteCount(current[..index]);

            if (_blockAesSalt.Length < aesSaltLength)
            {
                _blockAesSalt = new byte[aesSaltLength];
            }

            _blockAesSaltLength = Encoding.ASCII.GetBytes(current[..index], _blockAesSalt);

            current = current[(index + 1)..];

            var xorKeyLength = Encoding.ASCII.GetByteCount(current);

            if (_blockXorKey.Length < xorKeyLength)
            {
                _blockXorKey = new byte[xorKeyLength];
            }

            _blockXorKeyLength = Encoding.ASCII.GetBytes(current, _blockXorKey);

            using (var pbkdf1 = new PBKDF1(_blockAesPassword.AsSpan(0, _blockAesPasswordLength), _blockAesSalt.AsSpan(0, _blockAesSaltLength)))
            {
                pbkdf1.FillBytes(_blockAesKey.AsSpan(0, _blockAesKeySize / 8));
                pbkdf1.FillBytes(_blockAesIv);
            }

            if (_currentBlockIndication != _blockIndication)
            {
                _currentBlockIndication = _blockIndication;
                _isBlockFound = 0;

                Logger.PrintJob(_logger, "new job", $"{_pools[_poolIndex].Url}:{SeedNodePort}", _blockDifficulty, _blockMethod, _blockHeight);
            }
        }
        finally
        {
            _blockHeaderLock.ExitWriteLock();
        }

        return true;
    }
}