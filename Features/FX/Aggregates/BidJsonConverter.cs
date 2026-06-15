using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal sealed class BidJsonConverter : JsonConverter<Bid>
{
    public override Bid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Bid.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Bid value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
