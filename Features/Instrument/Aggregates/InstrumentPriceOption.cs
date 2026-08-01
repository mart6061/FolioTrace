using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentPriceOption : IInstrumentPrice
{
    public required InstrumentQuote Quote { get; init; }
    public required InstrumentPrice Last { get; init; }
    public string PriceType => nameof(InstrumentPriceOption);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentPriceOption(InstrumentQuote quote, InstrumentPrice last)
    {
        Quote = quote ?? throw new ArgumentNullException(nameof(quote));
        Last = last ?? throw new ArgumentNullException(nameof(last));
    }

    public InstrumentPrice Select(InstrumentPriceBasis basis) =>
        basis == InstrumentPriceBasis.Last ? Last : Quote.Select(basis);
}
