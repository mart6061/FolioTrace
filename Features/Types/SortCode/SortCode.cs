using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(SortCodeJsonConverter))]
public sealed record SortCode : IType
{
    public string Value { get; init; } = null!;

    public SortCode(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        value = value.Trim();
        if (!IsValid(value))
            throw new ArgumentException("Value must be a UK sort code in the format 123456 or 12-34-56.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private SortCode() { }

    internal static SortCode FromJson(string? value) => new(value!);

    private static bool IsValid(string value)
    {
        if (value.Length == 6)
            return value.All(IsDigit);

        return value.Length == 8 &&
            IsDigit(value[0]) &&
            IsDigit(value[1]) &&
            value[2] == '-' &&
            IsDigit(value[3]) &&
            IsDigit(value[4]) &&
            value[5] == '-' &&
            IsDigit(value[6]) &&
            IsDigit(value[7]);
    }

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    public static implicit operator string?(SortCode? sortCode) => sortCode?.Value;

    public static implicit operator SortCode(string s) => new(s);

    public override string ToString() => Value;
}
