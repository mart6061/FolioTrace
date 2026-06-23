using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

public sealed class TransactionLocalCostJsonConverter : JsonConverter<TransactionLocalCost>
{
    public override TransactionLocalCost Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TransactionLocalCost.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, TransactionLocalCost value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
