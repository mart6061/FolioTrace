using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(MidJsonConverter))]
public sealed record Mid : FXPriceValue
{
    public Mid(decimal value) : base(value) { }

    [JsonConstructor]
    private Mid() { }

    internal static new Mid FromJson(decimal value) => new(value);
}
