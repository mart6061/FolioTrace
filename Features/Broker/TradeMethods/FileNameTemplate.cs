using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

public sealed record FileNameTemplate
{
    public string Value { get; init; }

    [JsonConstructor]
    public FileNameTemplate(string value)
    {
        value = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value) || !value.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File name template must end with .xlsx.", nameof(value));
        var remainder = value.Replace("{brokername}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{yyyymmddhhmmssnn}", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (remainder.Contains('{') || remainder.Contains('}') || Path.GetInvalidFileNameChars().Any(remainder.Contains))
            throw new ArgumentException("File name template contains an unsupported token or invalid file-name character.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
}
