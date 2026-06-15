using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(TicketNumberJsonConverter))]
public sealed record TicketNumber : IType
{
    public int Value { get; init; }

    public TicketNumber(int value)
    {
        if (value <= 0)
            throw new ArgumentException("TicketNumber must be greater than zero.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private TicketNumber() { }

    internal static TicketNumber FromJson(int value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator TicketNumber(int value) => new(value);
}
