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
            var result = reader.GetString();
            return result == null ? BigInteger.Zero : BigInteger.Parse(result, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}