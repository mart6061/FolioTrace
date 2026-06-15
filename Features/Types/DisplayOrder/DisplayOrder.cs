using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(DisplayOrderJsonConverter))]
public sealed record DisplayOrder : IType
{
    public int Value { get; init; }

    public DisplayOrder(int value)
    {
        if (value < 0)
            throw new ArgumentException("DisplayOrder must not be negative.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private DisplayOrder() { }

    internal static DisplayOrder FromJson(int value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator DisplayOrder(int value) => new(value);

    public static implicit operator int(DisplayOrder displayOrder) => displayOrder?.Value ?? 0;
}
