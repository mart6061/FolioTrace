using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class LegalEntityIdentifierJsonConverter : JsonConverter<LegalEntityIdentifier>
{
    public override LegalEntityIdentifier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for LegalEntityIdentifier value.");

        return LegalEntityIdentifier.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, LegalEntityIdentifier value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
