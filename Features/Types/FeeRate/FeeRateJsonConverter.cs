using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class FeeRateJsonConverter : JsonConverter<FeeRate>
{
    public override FeeRate? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : FeeRate.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, FeeRate value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
