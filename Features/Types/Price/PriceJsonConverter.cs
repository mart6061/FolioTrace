using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class PriceJsonConverter : JsonConverter<Price>
{
    public override Price? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Price.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Price value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Amount);
}
