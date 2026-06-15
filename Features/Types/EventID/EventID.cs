using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(EventIDJsonConverter))]
public sealed record EventID : IType
{
    public Guid Value { get; init; }

    // Regular constructor enforces rules
    public EventID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private EventID() { }

    // Factory used by converter to create an instance without validation
    internal static EventID FromJson(string? value) => new EventID { Value = Guid.Parse(value!) };

    public static implicit operator Guid(EventID id) => id?.Value ?? Guid.Empty;

    public static implicit operator EventID(Guid g) => new EventID(g);

    public override string ToString() => Value.ToString();
}
