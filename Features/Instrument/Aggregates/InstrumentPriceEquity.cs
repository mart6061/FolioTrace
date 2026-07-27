using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentPriceEquity : IInstrumentPrice
{
    public required InstrumentQuote Quote { get; init; }

    /// <summary>The last traded price. Outside <see cref="Quote"/> because it does not participate in the spread.</summary>
    public required InstrumentPrice Last { get; init; }

    /// <summary>Net asset value. A fund valuation rather than a market quote, so it sits outside the spread.</summary>
    public required InstrumentPrice Nav { get; init; }

    public string PriceType => nameof(InstrumentPriceEquity);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentPriceEquity(InstrumentQuote quote, InstrumentPrice last, InstrumentPrice nav)
    {
        Quote = quote ?? throw new ArgumentNullException(nameof(quote));
        Last = last ?? throw new ArgumentNullException(nameof(last));
        Nav = nav ?? throw new ArgumentNullException(nameof(nav));
    }

    public InstrumentPrice Select(InstrumentPriceBasis basis) =>
        basis switch
        {
            InstrumentPriceBasis.Last => Last,
            InstrumentPriceBasis.NAV => Nav,
            _ => Quote.Select(basis)
        };
}
