using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class EventDateTimeJsonConverter : JsonConverter<EventDateTime>
{
    public override EventDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for EventDateTime value.");

        var s = reader.GetString();
        return EventDateTime.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, EventDateTime value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == default)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
    }
}
