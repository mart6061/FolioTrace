using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FolioTrace.Aggregates;

public sealed record TelephoneNumber
{
    private static readonly Regex Pattern = new("^[+0-9 ()-]{5,32}$", RegexOptions.Compiled);
    public string Value { get; init; }

    [JsonConstructor]
    public TelephoneNumber(string value)
    {
        value = value?.Trim() ?? string.Empty;
        if (!Pattern.IsMatch(value) || !value.Any(char.IsDigit))
            throw new ArgumentException("Telephone number is invalid.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
}
