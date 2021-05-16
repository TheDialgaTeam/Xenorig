using System;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Xirorig.Algorithm.Enums;
using Xirorig.Utility;

namespace Xirorig.Algorithm
{
    internal class XirobodAlgorithm : IAlgorithm
    {
        private readonly byte[] _networkBytes = { 0x73, 0x61, 0x6d, 0x20, 0x73, 0x65, 0x67, 0x75, 0x72, 0x61 };
        private byte[] _blockchainMarkKey = Array.Empty<byte>();

        public AlgorithmType AlgorithmType => AlgorithmType.Xirobod;

        public string BlockchainVersion => "01";

        public int BlockchainChecksum => 16;

        public int BlockchainSha512HexStringLength => 128;

        public byte[] BlockchainMarkKey
        {
            get
            {
                if (_blockchainMarkKey.Length == 0)
                {
                    _blockchainMarkKey = Encoding.ASCII.GetBytes(Convert.ToHexString(Sha3Utility.DoSha3512Hash(new Sha3Digest(512), _networkBytes)));
                }

                return _blockchainMarkKey;
            }
        }
    }
}