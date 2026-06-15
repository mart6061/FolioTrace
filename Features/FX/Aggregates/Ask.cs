using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(AskJsonConverter))]
public sealed record Ask : FXPriceValue
{
    public Ask(decimal value) : base(value) { }

    [JsonConstructor]
    private Ask() { }

    internal static new Ask FromJson(decimal value) => new(value);
}
