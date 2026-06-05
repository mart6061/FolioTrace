using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentPriceCash : IInstrumentPrice
{
    public required InstrumentPrice Price { get; init; }

    public string PriceType => nameof(InstrumentPriceCash);

    [SetsRequiredMembers]
    public InstrumentPriceCash()
        : this(new InstrumentPrice(1m))
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentPriceCash(InstrumentPrice price)
    {
        Price = price ?? throw new ArgumentNullException(nameof(price));

        if (Price.Amount != 1m)
            throw new ArgumentException("Cash price must be fixed at 1.", nameof(price));
    }
}
