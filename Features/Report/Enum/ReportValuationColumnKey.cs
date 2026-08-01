using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<ReportValuationColumnKey>))]
public enum ReportValuationColumnKey
{
    InstrumentName,
    OptionType,
    Underlying,
    Strike,
    Expiry,
    ExerciseStyle,
    SettlementType,
    ContractMultiplier,
    ExpiryStatus,
    ISIN,
    Sedol,
    QuotePrice,
    AccruedInterest,
    Quantity,
    CleanValue,
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
