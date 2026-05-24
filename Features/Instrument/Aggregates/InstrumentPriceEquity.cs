using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentPriceEquity : IInstrumentPrice
{
    public required InstrumentPrice Bid { get; init; }

    public required InstrumentPrice Mid { get; init; }

    public required InstrumentPrice Ask { get; init; }

    public required InstrumentPrice Nav { get; init; }

    public string PriceType => nameof(InstrumentPriceEquity);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentPriceEquity(InstrumentPrice bid, InstrumentPrice mid, InstrumentPrice ask, InstrumentPrice nav)
    {
        Bid = bid ?? throw new ArgumentNullException(nameof(bid));
        Mid = mid ?? throw new ArgumentNullException(nameof(mid));
        Ask = ask ?? throw new ArgumentNullException(nameof(ask));
        Nav = nav ?? throw new ArgumentNullException(nameof(nav));

        if (Bid.Amount.HasValue && Mid.Amount.HasValue && Bid.Amount > Mid.Amount)
            throw new ArgumentException("Bid must be less than or equal to mid.", nameof(bid));

        if (Mid.Amount.HasValue && Ask.Amount.HasValue && Mid.Amount > Ask.Amount)
            throw new ArgumentException("Mid must be less than or equal to ask.", nameof(mid));
    }

    public string ToData() => $"{Bid.ToData()}|{Mid.ToData()}|{Ask.ToData()}|{Nav.ToData()}";

    public string ToDetail() => $"{nameof(InstrumentPriceEquity)}: (Bid: {Bid.ToDetail()}, Mid: {Mid.ToDetail()}, Ask: {Ask.ToDetail()}, Nav: {Nav.ToDetail()})";
}
