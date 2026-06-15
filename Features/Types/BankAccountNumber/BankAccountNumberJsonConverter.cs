using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class BankAccountNumberJsonConverter : JsonConverter<BankAccountNumber>
{
    public override BankAccountNumber? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for BankAccountNumber value.");

        return BankAccountNumber.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, BankAccountNumber value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
