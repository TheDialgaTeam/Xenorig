using System.Text;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal partial class XenophyteCentralizedAlgorithm
{
    private readonly ReaderWriterLockSlim _blockHeaderLock = new();

    private int _blockId;
    private ulong _blockDifficulty;
    private long _blockTimestampCreate;

    private string _blockMethod = string.Empty;
    private string _blockIndication = string.Empty;
    
    private long _blockMinRange;
    private long _blockMaxRange;

    private int _blockAesRound;
    private int _blockAesSize;

    private string _blockKey = string.Empty;
    private string _blockAesKey = string.Empty;

    private string _blockXorKey = string.Empty;

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
                    _blockId = int.Parse(value);
                }
                else if (key.SequenceEqual("METHOD"))
                {
                    if (value.SequenceEqual(_blockMethod)) continue;
                    _blockMethod = value.ToString();
                }
                else if (key.SequenceEqual("DIFFICULTY"))
                {
                    _blockDifficulty = ulong.Parse(value);
                }
                else if (key.SequenceEqual("TIMESTAMP"))
                {
                    _blockTimestampCreate = long.Parse(value);
                }
                else if (key.SequenceEqual("KEY"))
                {
                    if (value.SequenceEqual(_blockKey)) continue;
                    _blockKey = value.ToString();
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

            _blockAesSize = int.Parse(current[..index]);

            current = current[(index + 1)..];

            index = current.IndexOf('#');
            if (index < 0) return false;

            if (!current[..index].SequenceEqual(_blockAesKey))
            {
                _blockAesKey = current[..index].ToString();
            }
            
            current = current[(index + 1)..];

            if (!current.SequenceEqual(_blockXorKey))
            {
                _blockXorKey = current.ToString();
            }

            if (_currentBlockIndication != _blockIndication)
            {
                _currentBlockIndication = _blockIndication;
                _isBlockFound = 0;

                Logger.PrintJob(_logger, "new job", $"{_pools[_poolIndex].Url}:{SeedNodePort}", _blockDifficulty, _blockMethod, _blockId);
            }
        }
        finally
        {
            _blockHeaderLock.ExitWriteLock();
        }

        return true;
    }
}