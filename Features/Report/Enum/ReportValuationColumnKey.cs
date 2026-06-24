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
    Quantity,
    BookValue,
    BookValueDefault,
    BookValueFIFO,
    BookValueLIFO,
    BookValueRunningAverage,
    BookCost,
    Weight,
    Target,
    Min,
    Max
}
