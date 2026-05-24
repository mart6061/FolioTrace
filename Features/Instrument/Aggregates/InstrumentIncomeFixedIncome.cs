using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIncomeFixedIncome : IInstrumentIncome
{
    public required ValuationPrice AccruedInterest { get; init; }

    public string IncomeType => nameof(InstrumentIncomeFixedIncome);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentIncomeFixedIncome(ValuationPrice accruedInterest)
    {
        AccruedInterest = accruedInterest ?? throw new ArgumentNullException(nameof(accruedInterest));
    }

    public string ToData() => AccruedInterest.ToData();

    public string ToDetail() => $"{nameof(InstrumentIncomeFixedIncome)}: (AccruedInterest: {AccruedInterest.ToDetail()})";
}
