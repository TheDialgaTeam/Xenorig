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

using System;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xirorig.Miner.Network.Api.JsonConverter
{
    internal class BigIntegerJsonConverter : JsonConverter<BigInteger>
    {
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var result = reader.GetString();
                    return result == null ? BigInteger.Zero : BigInteger.Parse(result, CultureInfo.InvariantCulture);

                case JsonTokenType.Number:
                    return new BigInteger(reader.GetUInt64());

                default:
                    throw new JsonException();
            }
        }

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
        {
            if (value.GetBitLength() <= 64)
            {
                writer.WriteNumberValue((ulong) value);
            }
            else
            {
                writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}