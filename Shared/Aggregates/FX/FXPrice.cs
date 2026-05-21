using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FXPrice : IType
{
    public required Bid Bid { get; init; }

    public required Mid Mid { get; init; }

    public required Ask Ask { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public FXPrice(Bid bid, Mid mid, Ask ask)
    {
        if (bid is null)
            throw new ArgumentNullException(nameof(bid));

        if (mid is null)
            throw new ArgumentNullException(nameof(mid));

        if (ask is null)
            throw new ArgumentNullException(nameof(ask));

        if (bid.Value > mid.Value)
            throw new ArgumentException("Bid must be less than or equal to mid.", nameof(bid));

        if (mid.Value > ask.Value)
            throw new ArgumentException("Mid must be less than or equal to ask.", nameof(mid));

        Bid = bid;
        Mid = mid;
        Ask = ask;
    }

    public string ToData() => $"{Bid.ToData()}|{Mid.ToData()}|{Ask.ToData()}";

    public string ToDetail() => $"{nameof(FXPrice)}: (Bid: {Bid}, Mid: {Mid}, Ask: {Ask})";
}
