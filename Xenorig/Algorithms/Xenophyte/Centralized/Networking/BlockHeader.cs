﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using Xenorig.Utilities.KeyDerivationFunction;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public sealed partial class BlockHeader
{
    public int BlockHeight => _blockHeight;

    public long BlockTimestampCreate => _blockTimestampCreate;

    public string BlockMethod => _blockMethod;

    public string BlockIndication => _blockIndication;

    public long BlockDifficulty => _blockDifficulty;

    public long BlockMinRange => _blockMinRange;

    public long BlockMaxRange => _blockMaxRange;

    public ReadOnlySpan<byte> XorKey => _xorKey.AsSpan(0, _xorKeyLength);

    public ReadOnlySpan<byte> AesKey => _aesKey.AsSpan(0, _aesKeyLength);

    public ReadOnlySpan<byte> AesIv => _aesIv.AsSpan();

    public int AesRound => _aesRound;
    
    private int _blockHeight;
    private long _blockTimestampCreate;
    
    private string _blockMethod = string.Empty;
    private string _blockIndication = string.Empty;
    
    private long _blockDifficulty;
    private long _blockMinRange;
    private long _blockMaxRange;
    
    private byte[] _xorKey = Array.Empty<byte>();
    private int _xorKeyLength;
    
    private readonly byte[] _aesKey = new byte[32];
    private int _aesKeyLength;
    
    private readonly byte[] _aesIv = new byte[16];
    private int _aesRound;

    private byte[] _aesPassword = Array.Empty<byte>();
    private int _aesPasswordLength;
    
    private byte[] _aesSalt = Array.Empty<byte>();
    private int _aesSaltLength;


    [GeneratedRegex("(?<key>[A-Za-z_]+)=(?<value>[A-Za-z0-9.;]+)&?")]
    private static partial Regex GetBlockHeaderKeyValuePairs();

    public bool UpdateBlockHeader(ReadOnlySpan<byte> packet)
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
        
        var stringPacket = Encoding.ASCII.GetString(packet);
        if (!stringPacket.StartsWith("SEND-CURRENT-BLOCK-MINING|")) return false;

        var matchCollection = GetBlockHeaderKeyValuePairs().Matches(stringPacket);

        if (matchCollection.Count == 0) return false;

        try
        {
            foreach (Match match in matchCollection)
            {
                switch (match.Groups["key"].Value)
                {
                    case "ID":
                        _blockHeight = int.Parse(match.Groups["value"].Value);
                        break;
                
                    case "TIMESTAMP":
                        _blockTimestampCreate = long.Parse(match.Groups["value"].Value);
                        break;
                
                    case "METHOD":
                        _blockMethod = match.Groups["value"].Value;
                        break;
                
                    case "INDICATION":
                        _blockIndication = match.Groups["value"].Value;
                        break;
                
                    case "DIFFICULTY":
                        _blockDifficulty = long.Parse(match.Groups["value"].Value);
                        break;

                    case "JOB":
                    {
                        var value = match.Groups["value"].ValueSpan;
                        var index = value.IndexOf(';');
                    
                        _blockMinRange = long.Parse(value[..index]);
                        _blockMaxRange = long.Parse(value[(index + 1)..]);
                        break;
                    }

                    case "KEY":
                    {
                        var aesPasswordLength = Encoding.UTF8.GetByteCount(match.Groups["value"].Value);

                        if (_aesPassword.Length < aesPasswordLength)
                        {
                            _aesPassword = GC.AllocateUninitializedArray<byte>(aesPasswordLength);
                        }

                        _aesPasswordLength = Encoding.UTF8.GetBytes(match.Groups["value"].Value, _aesPassword);
                        break;
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UpdateBlockMethod(ReadOnlySpan<byte> packet)
    {
        // SEND-CONTENT-BLOCK-METHOD|
        // 1#128#128#128
        // AESROUND#AESSIZE#AESKEY#XORKEY
        
        var stringPacket = Encoding.ASCII.GetString(packet);
        if (!stringPacket.StartsWith("SEND-CONTENT-BLOCK-METHOD|")) return false;

        var subString = stringPacket[(stringPacket.IndexOf('|') + 1)..].Split('#');

        try
        {
            _aesRound = int.Parse(subString[0]);
            _aesKeyLength = int.Parse(subString[1]) / 8;

            _xorKeyLength = Encoding.UTF8.GetByteCount(subString[3]);

            if (_xorKey.Length < _xorKeyLength)
            {
                _xorKey = GC.AllocateUninitializedArray<byte>(_xorKeyLength);
            }

            _xorKeyLength = Encoding.UTF8.GetBytes(subString[3], _xorKey);
        
            _aesSaltLength = Encoding.UTF8.GetByteCount(subString[2]);

            if (_aesSalt.Length < _aesSaltLength)
            {
                _aesSalt = GC.AllocateUninitializedArray<byte>(_aesSaltLength);
            }
        
            _aesSaltLength = Encoding.UTF8.GetByteCount(subString[2]);

            using var pbkdf1 = new PBKDF1(_aesPassword.AsSpan(0, _aesPasswordLength), _aesSalt.AsSpan(0, _aesSaltLength));

            pbkdf1.FillBytes(_aesKey.AsSpan(0, _aesKeyLength));
            pbkdf1.FillBytes(_aesIv);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}