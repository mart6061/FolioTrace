using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(ReportNodeIDJsonConverter))]
public sealed record ReportNodeID : IType
{
    public Guid Value { get; init; }

    public ReportNodeID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private ReportNodeID() { }

    internal static ReportNodeID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(ReportNodeID id) => id?.Value ?? Guid.Empty;

    public static implicit operator ReportNodeID(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
