using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class TicketNumberJsonConverter : JsonConverter<TicketNumber>
{
    public override TicketNumber? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : TicketNumber.FromJson(reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, TicketNumber value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
