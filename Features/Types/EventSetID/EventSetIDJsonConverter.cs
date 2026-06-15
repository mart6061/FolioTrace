using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class EventSetIDJsonConverter : JsonConverter<EventSetID>
{
    public override EventSetID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for EventSetID value.");

        return EventSetID.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, EventSetID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
