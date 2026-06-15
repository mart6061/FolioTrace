using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(UserIDJsonConverter))]
public sealed record UserID : IType
{
    public Guid Value { get; init; }

    // Regular constructor enforces rules
    public UserID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private UserID() { }

    // Factory used by converter to create an instance without validation
    internal static UserID FromJson(string? value) => new UserID { Value = Guid.Parse(value!) };

    public static implicit operator Guid(UserID id) => id?.Value ?? Guid.Empty;

    public static implicit operator UserID(Guid g) => new UserID(g);

    public override string ToString() => Value.ToString();
}
