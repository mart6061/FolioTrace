using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<BookCostSource>))]
public enum BookCostSource
{
    SameCurrency,
    ManualOverride,
    FxEstimate,
    Correction
}
