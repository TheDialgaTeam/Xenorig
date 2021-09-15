// Xirorig
// Copyright 2021 Yong Jian Ming
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Numerics;

namespace Xirorig.Algorithms.Xiropht.Decentralized.Api.JobResult.Models
{
    internal class MiningPowShare
    {
        public string WalletAddress { get; set; } = string.Empty;

        public long BlockHeight { get; set; }

        public string BlockHash { get; set; } = string.Empty;

        public string PoWaCShare { get; set; } = string.Empty;

        public long Nonce { get; set; }

        public string NonceComputedHexString { get; set; } = string.Empty;

        public BigInteger PoWaCShareDifficulty { get; set; }

        public long Timestamp { get; set; }
    }
}