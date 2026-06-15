using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal sealed class MidJsonConverter : JsonConverter<Mid>
{
    public override Mid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Mid.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Mid value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
