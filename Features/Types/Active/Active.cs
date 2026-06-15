using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(ActiveJsonConverter))]
public sealed record Active : IType
{
    public bool Value { get; init; }

    public Active(bool value)
    {
        Value = value;
    }

    [JsonConstructor]
    private Active() { }

    internal static Active FromJson(bool value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Active(bool value) => new(value);

    public static implicit operator bool(Active active) => active?.Value ?? false;
}
