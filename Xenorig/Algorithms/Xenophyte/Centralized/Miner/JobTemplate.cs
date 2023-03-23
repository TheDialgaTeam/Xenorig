using System;
using Xenorig.Algorithms.Xenophyte.Centralized.Networking;
using Xenorig.Utilities;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Miner;

public sealed class JobTemplate
{
    public int BlockHeight
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _blockHeight;
            }
        }
    }

    public long BlockTimestampCreate
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _blockTimestampCreate;
            }
        }
    }

    public string BlockMethod
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _blockMethod;
            }
        }
    }

    public string BlockIndication
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _blockIndication;
            }
        }
    }

    public long BlockDifficulty
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _blockDifficulty;
            }
        }
    }

    public long BlockMinRange
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _blockMinRange;
            }
        }
    }

    public long BlockMaxRange
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _blockMaxRange;
            }
        }
    }

    public byte[] XorKey
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _xorKey;
            }
        }
    }

    public int XorKeyLength
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _xorKeyLength;
            }
        }
    }

    public byte[] AesKey
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _aesKey;
            }
        }
    }

    public int AesKeyLength
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _aesKeyLength;
            }
        }
    }

    public byte[] AesIv
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _aesIv;
            }
        }
    }

    public int AesIvLength
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _aesIvLength;
            }
        }
    }

    public int AesRound
    {
        get
        {
            //lock (_synchronizationLock)
            {
                return _aesRound;
            }
        }
    }

    public bool BlockFound
    {
        get
        {
            lock (_synchronizationLock)
            {
                return _blockFound;
            }
        }
        set
        {
            lock (_synchronizationLock)
            {
                _blockFound = value;
            }
        }
    }

    private readonly object _synchronizationLock = new();

    private int _blockHeight;
    private long _blockTimestampCreate;

    private string _blockMethod = string.Empty;
    private string _blockIndication = string.Empty;

    private long _blockDifficulty;
    private long _blockMinRange;
    private long _blockMaxRange;

    private byte[] _xorKey = Array.Empty<byte>();
    private int _xorKeyLength;

    private byte[] _aesKey = Array.Empty<byte>();
    private int _aesKeyLength;

    private byte[] _aesIv = Array.Empty<byte>();
    private int _aesIvLength;

    private int _aesRound;

    private bool _blockFound;
    
    public void UpdateJobTemplate(in BlockHeader blockHeader)
    {
        lock (_synchronizationLock)
        {
            _blockHeight = blockHeader.Height;
            _blockTimestampCreate = blockHeader.TimestampCreate;
            _blockMethod = blockHeader.Method;
            _blockIndication = blockHeader.Indication;
            _blockDifficulty = blockHeader.Difficulty;
            _blockMinRange = blockHeader.MinRange;
            _blockMaxRange = blockHeader.MaxRange;

            var xorKeyLength = blockHeader.XorKey.Length;

            if (_xorKey.Length < xorKeyLength)
            {
                _xorKey = GC.AllocateUninitializedArray<byte>(xorKeyLength);
            }

            BufferUtility.MemoryCopy(blockHeader.XorKey, _xorKey, xorKeyLength);
            _xorKeyLength = xorKeyLength;

            var aesKeyLength = blockHeader.AesKey.Length;

            if (_aesKey.Length < blockHeader.AesKey.Length)
            {
                _aesKey = GC.AllocateUninitializedArray<byte>(aesKeyLength);
            }

            BufferUtility.MemoryCopy(blockHeader.AesKey, _aesKey, aesKeyLength);
            _aesKeyLength = aesKeyLength;

            var aesIvLength = blockHeader.AesIv.Length;

            if (_aesIv.Length < blockHeader.AesIv.Length)
            {
                _aesIv = GC.AllocateUninitializedArray<byte>(aesIvLength);
            }

            BufferUtility.MemoryCopy(blockHeader.AesIv, _aesIv, aesIvLength);
            _aesIvLength = aesIvLength;

            _aesRound = blockHeader.AesRound;

            _blockFound = false;
        }
    }
}