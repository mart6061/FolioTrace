using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<TradeFileColumn>))]
public enum TradeFileColumn
{
    TicketID,
    ISIN,
    Sedol,
    Quantity,
    Price,
    Currency,
    SecurityType,
    OptionType,
    UnderlyingInstrumentID,
    UnderlyingSymbol,
    UnderlyingISIN,
    StrikePrice,
    StrikeCurrency,
    ExpirationDate,
    ExerciseStyle,
    SettlementType,
    ContractMultiplier,
    GrossPremium
}
