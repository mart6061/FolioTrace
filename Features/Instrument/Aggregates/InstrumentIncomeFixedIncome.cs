using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIncomeFixedIncome : IInstrumentIncome
{
    public required InstrumentPrice AccruedInterest { get; init; }

    public string IncomeType => nameof(InstrumentIncomeFixedIncome);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentIncomeFixedIncome(InstrumentPrice accruedInterest)
    {
        AccruedInterest = accruedInterest ?? throw new ArgumentNullException(nameof(accruedInterest));
    }
}
