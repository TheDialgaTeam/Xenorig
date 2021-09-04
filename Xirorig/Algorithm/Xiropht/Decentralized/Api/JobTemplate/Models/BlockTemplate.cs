using System;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using Xirorig.Network.Api.JobTemplate;

namespace Xirorig.Algorithm.Xiropht.Decentralized.Api.JobTemplate.Models
{
    internal record BlockTemplate(
        long CurrentBlockHeight,
        BigInteger CurrentBlockDifficulty,
        string CurrentBlockHash,
        [property: JsonPropertyName("CurrentMiningPoWaCSetting")]
        MiningSettings MiningSettings) : IJobTemplate
    {
        private int _previousBlockTransactionCount = -1;
        private string _previousBlockFinalTransactionHash = string.Empty;

        public int PreviousBlockTransactionCount
        {
            get
            {
                if (_previousBlockTransactionCount != -1) return _previousBlockTransactionCount;

                var transactionCountHexString = CurrentBlockHash.AsSpan(32, 8);
                var result = 0;

                for (var i = 0; i < 4; i++)
                {
                    int high = transactionCountHexString[i * 2];
                    int low = transactionCountHexString[i * 2 + 1];

                    high = (high & 0xF) + ((high & 0x40) >> 6) * 9;
                    low = (low & 0xF) + ((low & 0x40) >> 6) * 9;

                    result += ((high << 4) + low) << (8 * i);
                }

                _previousBlockTransactionCount = result;
                return _previousBlockTransactionCount;
            }
        }

        public string PreviousBlockFinalTransactionHash
        {
            get
            {
                if (_previousBlockFinalTransactionHash != string.Empty) return _previousBlockFinalTransactionHash;

                var transactionCountHexString = CurrentBlockHash.AsSpan(40, 128);
                var result = new StringBuilder();

                for (var i = 0; i < 64; i++)
                {
                    int high = transactionCountHexString[i * 2];
                    int low = transactionCountHexString[i * 2 + 1];

                    high = (high & 0xF) + ((high & 0x40) >> 6) * 9;
                    low = (low & 0xF) + ((low & 0x40) >> 6) * 9;

                    result.AppendFormat("{0:X2}", (byte) ((high << 4) + low));
                }

                _previousBlockFinalTransactionHash = result.ToString();
                return _previousBlockFinalTransactionHash;
            }
        }
    }
}