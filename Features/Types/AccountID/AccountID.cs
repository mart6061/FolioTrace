using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(AccountIDJsonConverter))]
public sealed record AccountID : IType
{
    public Guid Value { get; init; }

    public AccountID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private AccountID() { }

    internal static AccountID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(AccountID id) => id?.Value ?? Guid.Empty;

    public static implicit operator AccountID(Guid g) => new(g);

    public override string ToString() => Value.ToString();
}
