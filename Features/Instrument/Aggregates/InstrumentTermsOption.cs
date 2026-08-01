using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentTermsOption : IInstrumentTerms
{
    public required OptionType OptionType { get; init; }
    public required InstrumentID UnderlyingInstrumentID { get; init; }
    public required Money StrikePrice { get; init; }
    public required InstrumentDate ExpirationDate { get; init; }
    public required OptionExerciseStyle ExerciseStyle { get; init; }
    public required OptionSettlementType SettlementType { get; init; }
    public required ContractMultiplier ContractMultiplier { get; init; }
    public string TermsType => nameof(InstrumentTermsOption);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentTermsOption(
        OptionType optionType,
        InstrumentID underlyingInstrumentID,
        Money strikePrice,
        InstrumentDate expirationDate,
        OptionExerciseStyle exerciseStyle,
        OptionSettlementType settlementType,
        ContractMultiplier contractMultiplier)
    {
        if (underlyingInstrumentID is null)
            throw new ArgumentNullException(nameof(underlyingInstrumentID));
        if (strikePrice is null)
            throw new ArgumentNullException(nameof(strikePrice));
        if (strikePrice.Amount <= 0m)
            throw new ArgumentException("Option strike price must be greater than zero.", nameof(strikePrice));
        if (expirationDate?.Value is null)
            throw new ArgumentException("Option expiration date is required.", nameof(expirationDate));

        OptionType = optionType;
        UnderlyingInstrumentID = underlyingInstrumentID;
        StrikePrice = strikePrice;
        ExpirationDate = expirationDate;
        ExerciseStyle = exerciseStyle;
        SettlementType = settlementType;
        ContractMultiplier = contractMultiplier ?? throw new ArgumentNullException(nameof(contractMultiplier));
    }
}
