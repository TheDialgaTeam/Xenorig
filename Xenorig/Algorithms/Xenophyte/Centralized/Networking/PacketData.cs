using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Xenorig.Utilities;
using Xenorig.Utilities.Buffer;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public delegate void ReceivePacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime);

public sealed class PacketData : IDisposable
{
    private const byte PaddingCharacter = (byte) '*';

    private readonly ArrayOwner<byte> _packetArrayOwner;
    private readonly bool _isEncrypted;
    private readonly ReceivePacketHandler? _receivePacketHandler;

    public PacketData(ArrayOwner<byte> packetArrayOwner, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packetArrayOwner = packetArrayOwner;
        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }
    
    public PacketData(ReadOnlySpan<byte> packet, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packetArrayOwner = ArrayOwner<byte>.Rent(packet.Length);
        packet.CopyTo(_packetArrayOwner.Span);
        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }

    public PacketData(string packet, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packetArrayOwner = ArrayOwner<byte>.Rent(Encoding.ASCII.GetByteCount(packet));
        Encoding.UTF8.GetBytes(packet, _packetArrayOwner.Span);
        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }

    public bool Execute(NetworkStream networkStream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        var executeTimestamp = Stopwatch.GetTimestamp();
        return TryExecuteWrite(networkStream, key, iv) && TryExecuteRead(networkStream, key, iv, executeTimestamp);
    }
    
    private bool TryExecuteWrite(Stream stream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        try
        {
            if (!_isEncrypted)
            {
                stream.Write(_packetArrayOwner.Span);
            }
            else
            {
                var packet = _packetArrayOwner.Span;
                var packetLength = packet.Length;
                
                using var encryptedPacketArrayOwner = ArrayOwner<byte>.Rent(packetLength + (16 - packetLength % 16));
                var encryptedPacket = encryptedPacketArrayOwner.Span;
                
                if (SymmetricAlgorithmUtility.Encrypt_AES_256_CFB_8(key, iv, packet, encryptedPacket) == 0)
                {
                    return false;
                }

                using var base64EncryptedPacketArrayOwner = ArrayOwner<byte>.Rent(Base64Utility.EncodeLength(encryptedPacket) + 1);
                var base64EncryptedPacket = base64EncryptedPacketArrayOwner.Span;

                var bytesWritten = Base64Utility.Encode(encryptedPacket, base64EncryptedPacket);
                if (bytesWritten == 0) return false;

                base64EncryptedPacket.GetRef(bytesWritten) = PaddingCharacter;
                stream.Write(base64EncryptedPacket);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    [SkipLocalsInit]
    private bool TryExecuteRead(NetworkStream networkStream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, long executeTimestamp)
    {
        try
        {
            if (_receivePacketHandler == null) return true;

            using var receivedPacketArrayOwner = ArrayOwner<byte>.Rent(networkStream.Socket.ReceiveBufferSize);
            var receivedPacket = receivedPacketArrayOwner.Span;
            
            var bytesRead = networkStream.Read(receivedPacket);
            if (bytesRead == 0) return false;

            ReadOnlySpan<byte> base64EncryptedPacket = receivedPacket[..bytesRead];
            if (base64EncryptedPacket.GetRef(bytesRead - 1) != PaddingCharacter) return false;

            base64EncryptedPacket = base64EncryptedPacket[..(bytesRead - 1)];

            using var encryptedPacketArrayOwner = ArrayOwner<byte>.Rent(Base64Utility.DecodeLength(base64EncryptedPacket));
            var encryptedPacket = encryptedPacketArrayOwner.Span;
            var decodedBytes = Base64Utility.Decode(base64EncryptedPacket, encryptedPacket);
            if (decodedBytes == 0) return false;

            using var decryptedPacketArrayOwner = ArrayOwner<byte>.Rent(encryptedPacket.Length);
            var decryptedPacket = decryptedPacketArrayOwner.Span;
            var bytesWritten = SymmetricAlgorithmUtility.Decrypt_AES_256_CFB_8(key, iv, encryptedPacket, decryptedPacket);
            if (bytesWritten == 0) return false;

            _receivePacketHandler(decryptedPacket[..^decryptedPacket[^1]], Stopwatch.GetElapsedTime(executeTimestamp));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _packetArrayOwner.Dispose();
    }
}