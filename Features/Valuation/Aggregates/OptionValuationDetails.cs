using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record OptionValuationDetails(
    OptionType OptionType,
    InstrumentID UnderlyingInstrumentID,
    string UnderlyingInstrumentName,
    decimal StrikePrice,
    Alpha3 StrikeCurrency,
    DateOnly ExpirationDate,
    OptionExerciseStyle ExerciseStyle,
    OptionSettlementType SettlementType,
    decimal ContractMultiplier,
    bool Expired);
