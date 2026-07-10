using System.Text.Json.Serialization;

namespace FolioTrace.Types;

public sealed record StoredFileID
{
    public Guid Value { get; init; }
    [JsonConstructor]
    public StoredFileID(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));
        Value = value;
    }
    public static implicit operator Guid(StoredFileID id) => id.Value;
    public static implicit operator StoredFileID(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
