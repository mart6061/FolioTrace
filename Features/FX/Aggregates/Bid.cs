using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(BidJsonConverter))]
public sealed record Bid : FXPriceValue
{
    public Bid(decimal value) : base(value) { }

    [JsonConstructor]
    private Bid() { }

    internal static new Bid FromJson(decimal value) => new(value);
}
