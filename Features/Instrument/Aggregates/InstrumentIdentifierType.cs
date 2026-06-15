using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<InstrumentIdentifierType>))]
public enum InstrumentIdentifierType
{
    Sedol,
    ISIN,
    Ticker,
    CUSIP,
    FIGI,
    RIC
}
