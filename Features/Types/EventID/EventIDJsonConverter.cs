using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class EventIDJsonConverter : JsonConverter<EventID>
{
    public override EventID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for EventID value.");

        var s = reader.GetString();
        return EventID.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, EventID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
