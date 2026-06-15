using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class CouponRateJsonConverter : JsonConverter<CouponRate>
{
    public override CouponRate? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : CouponRate.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, CouponRate value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
