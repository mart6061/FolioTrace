using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class TransactionBookCostJsonConverter : JsonConverter<TransactionBookCost>
{
    public override TransactionBookCost? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : TransactionBookCost.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, TransactionBookCost value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
