using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class InstrumentDateJsonConverter : JsonConverter<InstrumentDate>
{
    public override InstrumentDate? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for InstrumentDate value.");

        return InstrumentDate.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, InstrumentDate value, JsonSerializerOptions options)
    {
        if (value is null || !value.Value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
