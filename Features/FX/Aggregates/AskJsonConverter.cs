using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal sealed class AskJsonConverter : JsonConverter<Ask>
{
    public override Ask? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Ask.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Ask value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
