using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class InstrumentIDJsonConverter : JsonConverter<InstrumentID>
{
    public override InstrumentID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for InstrumentID value.");

        var s = reader.GetString();
        return InstrumentID.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, InstrumentID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
