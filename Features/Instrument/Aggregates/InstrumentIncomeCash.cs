using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIncomeCash : IInstrumentIncome
{
    public required Yield Income { get; init; }

    public string IncomeType => nameof(InstrumentIncomeCash);

    [SetsRequiredMembers]
    public InstrumentIncomeCash()
        : this(new Yield(0m))
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentIncomeCash(Yield income)
    {
        Income = income ?? throw new ArgumentNullException(nameof(income));

        if (Income.Value != 0m)
            throw new ArgumentException("Cash income yield must be zero.", nameof(income));
    }
}
