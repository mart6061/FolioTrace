using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(InstrumentIDJsonConverter))]
public sealed record InstrumentID : IType
{
    public Guid Value { get; init; }

    // Regular constructor enforces rules
    public InstrumentID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private InstrumentID() { }

    // Factory used by converter to create an instance without validation
    internal static InstrumentID FromJson(string? value) => new InstrumentID { Value = Guid.Parse(value!) };

    public static implicit operator Guid(InstrumentID id) => id?.Value ?? Guid.Empty;

    public static implicit operator InstrumentID(Guid g) => new InstrumentID(g);

    public override string ToString() => Value.ToString();
}
