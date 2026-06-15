using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class YieldJsonConverter : JsonConverter<Yield>
{
    public override Yield? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Yield.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Yield value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
