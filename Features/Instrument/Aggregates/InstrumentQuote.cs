using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

/// <summary>
/// The ordered bid/mid/ask triple. Equities quote these directly; fixed income quotes them clean, with the
/// dirty equivalents derived by adding accrued interest on <see cref="InstrumentValue"/>.
/// </summary>
/// <remarks>
/// This type carries exactly the quotes the ordering rule governs. Last traded price and net asset value are
/// deliberately absent: neither participates in the spread, and neither applies to a bond.
/// </remarks>
public sealed record InstrumentQuote : IType
{
    public required InstrumentPrice Bid { get; init; }

    public required InstrumentPrice Mid { get; init; }

    public required InstrumentPrice Ask { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentQuote(InstrumentPrice bid, InstrumentPrice mid, InstrumentPrice ask)
    {
        Bid = bid ?? throw new ArgumentNullException(nameof(bid));
        Mid = mid ?? throw new ArgumentNullException(nameof(mid));
        Ask = ask ?? throw new ArgumentNullException(nameof(ask));

        if (Bid.Amount.HasValue && Mid.Amount.HasValue && Bid.Amount > Mid.Amount)
            throw new ArgumentException("Bid must be less than or equal to mid.", nameof(bid));

        if (Mid.Amount.HasValue && Ask.Amount.HasValue && Mid.Amount > Ask.Amount)
            throw new ArgumentException("Mid must be less than or equal to ask.", nameof(mid));
    }

    /// <summary>
    /// Adds a constant to every quote, for deriving dirty prices from clean ones. Ordering is preserved because
    /// the same amount shifts all three, so the result needs no revalidation.
    /// </summary>
    public InstrumentQuote Add(InstrumentPrice amount) =>
        new(Offset(Bid, amount), Offset(Mid, amount), Offset(Ask, amount));

    public InstrumentPrice Select(InstrumentPriceBasis basis) =>
        basis switch
        {
            InstrumentPriceBasis.Bid => Bid,
            InstrumentPriceBasis.Ask => Ask,
            _ => Mid
        };

    private static InstrumentPrice Offset(InstrumentPrice price, InstrumentPrice? amount) =>
        new(price.Amount.HasValue ? price.Amount + (amount?.Amount ?? 0m) : null);
}
