using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record OptionExecutionDetails(
    OptionType OptionType,
    InstrumentID UnderlyingInstrumentID,
    string UnderlyingSymbol,
    string UnderlyingSecurityID,
    string UnderlyingSecurityIDSource,
    Money StrikePrice,
    InstrumentDate ExpirationDate,
    OptionExerciseStyle ExerciseStyle,
    OptionSettlementType SettlementType,
    ContractMultiplier ContractMultiplier);
