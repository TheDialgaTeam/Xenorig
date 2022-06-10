using System.Buffers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Xenorig.Utilities;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal delegate void ReceivePacketHandler(ReadOnlySpan<byte> packet, double roundTripTime);

internal readonly struct PacketData
{
    private const byte PaddingCharacter = (byte) '*';

    private readonly byte[] _packet;
    private readonly int _packetLength;

    private readonly bool _isEncrypted;

    private readonly ReceivePacketHandler? _receivePacketHandler;

    public PacketData(ReadOnlySpan<byte> packet, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packet = ArrayPool<byte>.Shared.Rent(packet.Length);
        _packetLength = packet.Length;

        packet.CopyTo(_packet);

        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }

    public PacketData(string packet, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        var byteCount = Encoding.ASCII.GetByteCount(packet);

        _packet = ArrayPool<byte>.Shared.Rent(byteCount);
        _packetLength = byteCount;

        Encoding.ASCII.GetBytes(packet, _packet);

        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }

    public bool Execute(NetworkStream networkStream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, out Exception? exception)
    {
        var executeTimestamp = DateTime.Now;

        try
        {
            return TryExecuteWrite(networkStream, key, iv, out exception) && TryExecuteRead(networkStream, key, iv, executeTimestamp, out exception);
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    [SkipLocalsInit]
    private bool TryExecuteWrite(NetworkStream networkStream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, out Exception? exception)
    {
        try
        {
            if (!_isEncrypted)
            {
                networkStream.Write(_packet.AsSpan(0, _packetLength));
            }
            else
            {
                Span<byte> encryptedPacket = stackalloc byte[_packetLength + (16 - _packetLength % 16)];

                if (SymmetricAlgorithmUtility.Encrypt_AES_256_CFB_8(key, iv, _packet.AsSpan(0, _packetLength), encryptedPacket) == 0)
                {
                    exception = new Exception("Error encrypting packet.");
                    return false;
                }

                Span<byte> base64EncryptedPacket = stackalloc byte[Base64Utility.EncodeLength(encryptedPacket) + 1];
                var bytesWritten = Base64Utility.Encode(encryptedPacket, base64EncryptedPacket);

                if (bytesWritten == 0)
                {
                    exception = new Exception("Error encoding packet.");
                    return false;
                }

                base64EncryptedPacket[bytesWritten] = PaddingCharacter;

                networkStream.Write(base64EncryptedPacket);
            }

            exception = null;
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(_packet);
        }
    }

    [SkipLocalsInit]
    private bool TryExecuteRead(NetworkStream networkStream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, DateTime executeTimestamp, out Exception? exception)
    {
        if (_receivePacketHandler == null)
        {
            exception = null;
            return true;
        }

        Span<byte> receivedPacket = stackalloc byte[networkStream.Socket.ReceiveBufferSize];
        var bytesRead = networkStream.Read(receivedPacket);

        if (bytesRead < 0)
        {
            exception = new EndOfStreamException();
            return false;
        }

        ReadOnlySpan<byte> base64EncryptedPacket = receivedPacket[..bytesRead];

        if (base64EncryptedPacket[bytesRead - 1] != PaddingCharacter)
        {
            exception = new Exception("Invalid packet received.");
            return false;
        }

        base64EncryptedPacket = base64EncryptedPacket[..(bytesRead - 1)];

        Span<byte> encryptedPacket = stackalloc byte[Base64Utility.DecodeLength(base64EncryptedPacket)];
        var decodedBytes = Base64Utility.Decode(base64EncryptedPacket, encryptedPacket);

        if (decodedBytes == 0)
        {
            exception = new Exception("Error decoding packet.");
            return false;
        }

        Span<byte> decryptedPacket = stackalloc byte[encryptedPacket.Length];
        var bytesWritten = SymmetricAlgorithmUtility.Decrypt_AES_256_CFB_8(key, iv, encryptedPacket, decryptedPacket);

        if (bytesWritten == 0)
        {
            exception = new Exception("Error decrypting packet.");
            return false;
        }

        _receivePacketHandler(decryptedPacket[..^decryptedPacket[^1]], (DateTime.Now - executeTimestamp).TotalMilliseconds);

        exception = null;
        return true;
    }
}