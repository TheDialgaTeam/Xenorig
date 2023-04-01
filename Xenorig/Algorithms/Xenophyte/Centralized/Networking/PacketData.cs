using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xenorig.Utilities;
using Xenorig.Utilities.Buffer;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public delegate void ReceivePacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime);

[DebuggerDisplay("{ToString(),raw}")]
public sealed class PacketData : IDisposable
{
    private const byte PaddingCharacter = (byte) '*';

    private readonly ArrayPoolOwner<byte> _packetArrayPoolOwner;
    private readonly bool _isEncrypted;
    private readonly ReceivePacketHandler? _receivePacketHandler;

    public PacketData(ArrayPoolOwner<byte> packetArrayPoolOwner, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packetArrayPoolOwner = packetArrayPoolOwner;
        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }
    
    public PacketData(ReadOnlySpan<byte> packet, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packetArrayPoolOwner = ArrayPoolOwner<byte>.Rent(packet.Length);
        BufferUtility.MemoryCopy(packet, _packetArrayPoolOwner.Span, packet.Length);
        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }

    public PacketData(string packet, bool isEncrypted, ReceivePacketHandler? receivePacketHandler = null)
    {
        _packetArrayPoolOwner = ArrayPoolOwner<byte>.Rent(Encoding.UTF8.GetByteCount(packet));
        Encoding.UTF8.GetBytes(packet, _packetArrayPoolOwner.Span);
        _isEncrypted = isEncrypted;
        _receivePacketHandler = receivePacketHandler;
    }

    ~PacketData()
    {
        Dispose();
    }

    public async Task<bool> ExecuteAsync(NetworkStream networkStream, ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, CancellationToken cancellationToken)
    {
        var executeTimestamp = Stopwatch.GetTimestamp();
        return await TryExecuteWriteAsync(networkStream, key, iv, cancellationToken) && await TryExecuteReadAsync(networkStream, key, iv, executeTimestamp, cancellationToken);
    }

    private async Task<bool> TryExecuteWriteAsync(Stream stream, ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, CancellationToken cancellationToken)
    {
        if (!_isEncrypted)
        {
            await stream.WriteAsync(_packetArrayPoolOwner.Memory, cancellationToken);
        }
        else
        {
            var packetLength = _packetArrayPoolOwner.Span.Length;
            using var encryptedPacketArrayPoolOwner = ArrayPoolOwner<byte>.Rent(packetLength + (16 - packetLength % 16));

            if (SymmetricAlgorithmUtility.Encrypt_AES_256_CFB_8(key.Span, iv.Span, _packetArrayPoolOwner.Span, encryptedPacketArrayPoolOwner.Span) == 0)
            {
                return false;
            }

            using var base64EncryptedPacketArrayPoolOwner = ArrayPoolOwner<byte>.Rent(Base64Utility.EncodeLength(encryptedPacketArrayPoolOwner.Span) + 1);

            var bytesWritten = Base64Utility.Encode(encryptedPacketArrayPoolOwner.Span, base64EncryptedPacketArrayPoolOwner.Span);
            if (bytesWritten == 0) return false;

            base64EncryptedPacketArrayPoolOwner.Span.GetRef(bytesWritten) = PaddingCharacter;
            await stream.WriteAsync(base64EncryptedPacketArrayPoolOwner.Memory, cancellationToken);
        }

        return true;
    }

    private async Task<bool> TryExecuteReadAsync(NetworkStream networkStream, ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, long executeTimestamp, CancellationToken cancellationToken)
    {
        if (_receivePacketHandler == null) return true;

        using var receivedPacketArrayOwner = ArrayPoolOwner<byte>.Rent(networkStream.Socket.ReceiveBufferSize);
        var receivedPacket = receivedPacketArrayOwner.Memory;

        var bytesRead = await networkStream.ReadAsync(receivedPacket, cancellationToken);
        if (bytesRead == 0) return false;

        ReadOnlyMemory<byte> base64EncryptedPacket = receivedPacket[..bytesRead];
        if (base64EncryptedPacket.Span.GetRef(bytesRead - 1) != PaddingCharacter) return false;

        base64EncryptedPacket = base64EncryptedPacket[..(bytesRead - 1)];

        using var encryptedPacketArrayOwner = ArrayPoolOwner<byte>.Rent(Base64Utility.DecodeLength(base64EncryptedPacket.Span));
        var decodedBytes = Base64Utility.Decode(base64EncryptedPacket.Span, encryptedPacketArrayOwner.Span);
        if (decodedBytes == 0) return false;

        using var decryptedPacketArrayOwner = ArrayPoolOwner<byte>.Rent(encryptedPacketArrayOwner.Span.Length);
        var bytesWritten = SymmetricAlgorithmUtility.Decrypt_AES_256_CFB_8(key.Span, iv.Span, encryptedPacketArrayOwner.Span, decryptedPacketArrayOwner.Span);
        if (bytesWritten == 0) return false;

        _receivePacketHandler(decryptedPacketArrayOwner.Span[..bytesWritten], Stopwatch.GetElapsedTime(executeTimestamp));
        return true;
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(_packetArrayPoolOwner.Span);
    }

    public void Dispose()
    {
        _packetArrayPoolOwner.Dispose();
        GC.SuppressFinalize(this);
    }
}