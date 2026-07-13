using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

public sealed record SecretReference
{
    public string Value { get; init; }

    [JsonConstructor]
    public SecretReference(string value)
    {
        value = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
            throw new ArgumentException("Secret reference is required and must not exceed 128 characters.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
}
