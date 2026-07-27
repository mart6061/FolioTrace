using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentPriceFixedIncome : IInstrumentPrice
{
    /// <summary>
    /// Clean quotes, excluding accrued interest. The dirty equivalents are derived on
    /// <see cref="InstrumentValue"/>, which can see the paired accrued interest; they are never stored, because
    /// accrued interest moves with the valuation date and a stored dirty price would drift from it.
    /// </summary>
    public required InstrumentQuote CleanQuote { get; init; }

    public string PriceType => nameof(InstrumentPriceFixedIncome);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentPriceFixedIncome(InstrumentQuote cleanQuote)
    {
        CleanQuote = cleanQuote ?? throw new ArgumentNullException(nameof(cleanQuote));
    }
}
