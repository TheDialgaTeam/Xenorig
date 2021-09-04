using System;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xirorig.Network.Api.JsonConverter
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