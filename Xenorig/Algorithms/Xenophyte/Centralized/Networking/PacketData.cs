using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Xenorig.Utilities;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public delegate void ReceivePacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime);

public sealed class PacketData
{
    private const byte PaddingCharacter = (byte) '*';

    private readonly byte[] _packetBuffer;
    private readonly int _packetLength;

    private readonly bool _isEncrypted;

    private readonly ReceivePacketHandler? _receivePacketHandler;

    private ReadOnlySpan<byte> Packet => _packetBuffer.AsSpan(0, _packetLength);

    public PacketData(ReadOnlySpan<byte> packet, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packetBuffer = ArrayPool<byte>.Shared.Rent(packet.Length);
        _packetLength = packet.Length;
        BufferUtility.MemoryCopy(packet, _packetBuffer, packet.Length);
        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }

    public PacketData(string packet, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packetBuffer = ArrayPool<byte>.Shared.Rent(Encoding.ASCII.GetByteCount(packet));
        _packetLength = Encoding.ASCII.GetBytes(packet, _packetBuffer);
        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }

    public bool Execute(NetworkStream networkStream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        var executeTimestamp = DateTime.Now;
        return TryExecuteWrite(networkStream, key, iv) && TryExecuteRead(networkStream, key, iv, executeTimestamp);
    }

    [SkipLocalsInit]
    private bool TryExecuteWrite(Stream stream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        try
        {
            if (!_isEncrypted)
            {
                stream.Write(Packet);
            }
            else
            {
                Span<byte> encryptedPacket = stackalloc byte[_packetLength + (16 - _packetLength % 16)];

                if (SymmetricAlgorithmUtility.Encrypt_AES_256_CFB_8(key, iv, Packet, encryptedPacket) == 0)
                {
                    return false;
                }

                Span<byte> base64EncryptedPacket = stackalloc byte[Base64Utility.EncodeLength(encryptedPacket) + 1];
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
        finally
        {
            ArrayPool<byte>.Shared.Return(_packetBuffer);
        }
    }

    [SkipLocalsInit]
    private bool TryExecuteRead(NetworkStream networkStream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, DateTime executeTimestamp)
    {
        try
        {
            if (_receivePacketHandler == null) return true;
            
            Span<byte> receivedPacket = stackalloc byte[networkStream.Socket.ReceiveBufferSize];
            var bytesRead = networkStream.Read(receivedPacket);
            if (bytesRead == 0) return false;

            ReadOnlySpan<byte> base64EncryptedPacket = receivedPacket[..bytesRead];
            if (base64EncryptedPacket.GetRef(bytesRead - 1) != PaddingCharacter) return false;

            base64EncryptedPacket = base64EncryptedPacket[..(bytesRead - 1)];

            Span<byte> encryptedPacket = stackalloc byte[Base64Utility.DecodeLength(base64EncryptedPacket)];
            var decodedBytes = Base64Utility.Decode(base64EncryptedPacket, encryptedPacket);
            if (decodedBytes == 0) return false;

            Span<byte> decryptedPacket = stackalloc byte[encryptedPacket.Length];
            var bytesWritten = SymmetricAlgorithmUtility.Decrypt_AES_256_CFB_8(key, iv, encryptedPacket, decryptedPacket);
            if (bytesWritten == 0) return false;

            _receivePacketHandler(decryptedPacket[..^decryptedPacket[^1]], DateTime.Now - executeTimestamp);
            return true;
        }
        catch
        {
            return false;
        }
    }
}