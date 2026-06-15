using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<ReportValuationColumnKey>))]
public enum ReportValuationColumnKey
{
    InstrumentName,
    ISIN,
    Sedol,
    QuotePrice,
    BookValue,
    BookCost,
    Weight,
    Target,
    Min,
    Max
}
