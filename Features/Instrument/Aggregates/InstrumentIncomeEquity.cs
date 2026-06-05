using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIncomeEquity : IInstrumentIncome
{
    public required InstrumentPrice DividendAmount { get; init; }

    public required string DividendType { get; init; }

    public required InstrumentDate ExDividend { get; init; }

    public required InstrumentDate Declaration { get; init; }

    public required InstrumentDate Record { get; init; }

    public required InstrumentDate Payable { get; init; }

    public string IncomeType => nameof(InstrumentIncomeEquity);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentIncomeEquity(InstrumentPrice dividendAmount, string dividendType, InstrumentDate exDividend, InstrumentDate declaration, InstrumentDate record, InstrumentDate payable)
    {
        DividendAmount = dividendAmount ?? throw new ArgumentNullException(nameof(dividendAmount));
        DividendType = dividendType ?? throw new ArgumentNullException(nameof(dividendType));
        ExDividend = exDividend ?? throw new ArgumentNullException(nameof(exDividend));
        Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        Record = record ?? throw new ArgumentNullException(nameof(record));
        Payable = payable ?? throw new ArgumentNullException(nameof(payable));
    }
}
