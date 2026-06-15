using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(HoldingIDJsonConverter))]
public sealed record HoldingID : IType
{
    public Guid Value { get; init; }

    public HoldingID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private HoldingID() { }

    internal static HoldingID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(HoldingID id) => id?.Value ?? Guid.Empty;

    public static implicit operator HoldingID(Guid g) => new(g);

    public override string ToString() => Value.ToString();
}
