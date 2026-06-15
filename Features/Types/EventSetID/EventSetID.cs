using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(EventSetIDJsonConverter))]
public sealed record EventSetID : IType
{
    public Guid Value { get; init; }

    public EventSetID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private EventSetID() { }

    internal static EventSetID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(EventSetID id) => id?.Value ?? Guid.Empty;

    public static implicit operator EventSetID(Guid g) => new(g);

    public override string ToString() => Value.ToString();
}
