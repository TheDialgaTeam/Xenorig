using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TheDialgaTeam.Xiropht.Xirorig.Network
{
    public class ConcurrentNetworkPacketQueue : IDisposable
    {
        private readonly BlockingCollection<PacketToSend> _loginBlockingCollection = new BlockingCollection<PacketToSend>();
        private readonly BlockingCollection<PacketToSend> _submitBlockBlockingCollection = new BlockingCollection<PacketToSend>();
        private readonly BlockingCollection<PacketToSend> _receiveBlockBlockingCollection = new BlockingCollection<PacketToSend>();

        private readonly BlockingCollection<PacketToSend>[] _blockingCollections = new BlockingCollection<PacketToSend>[3];

        public ConcurrentNetworkPacketQueue()
        {
            _blockingCollections[0] = _loginBlockingCollection;
            _blockingCollections[1] = _submitBlockBlockingCollection;
            _blockingCollections[2] = _receiveBlockBlockingCollection;
        }

        public void Enqueue(string packet, bool isEncrypted, PacketType packetType)
        {
            var blockingCollection = packetType switch
            {
                PacketType.Login => _loginBlockingCollection,
                PacketType.SubmitBlock => _submitBlockBlockingCollection,
                PacketType.ReceiveBlockTemplate => _receiveBlockBlockingCollection,
                var _ => throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null)
            };

            blockingCollection.Add(new PacketToSend(packet, isEncrypted));
        }

        public PacketToSend Dequeue(CancellationToken cancellationToken = default)
        {
            BlockingCollection<PacketToSend>.TakeFromAny(_blockingCollections, out var packet, cancellationToken);
            return packet;
        }

        public void Dispose()
        {
            _loginBlockingCollection.Dispose();
            _submitBlockBlockingCollection.Dispose();
            _receiveBlockBlockingCollection.Dispose();
        }
    }
}