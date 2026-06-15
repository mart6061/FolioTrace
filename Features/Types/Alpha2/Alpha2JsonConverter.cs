using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class Alpha2JsonConverter : JsonConverter<Alpha2>
{
    public override Alpha2? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for Alpha2 value.");

        var s = reader.GetString();
        return Alpha2.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, Alpha2 value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
