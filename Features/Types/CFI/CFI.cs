using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(CFIJsonConverter))]
public sealed record CFI : IType
{
    public string Value { get; init; } = null!;

    public char CategoryCode => Value[0];

    public char GroupCode => Value[1];

    public char Attribute1 => Value[2];

    public char Attribute2 => Value[3];

    public char Attribute3 => Value[4];

    public char Attribute4 => Value[5];

    public CFICategory Category => CFICategory.From(CategoryCode);

    public CFIGroup Group => CFIGroup.From(CategoryCode, GroupCode);

    public bool IsEquity => CategoryCode == 'E';

    public bool IsDebt => CategoryCode == 'D';

    public bool IsOption => CategoryCode == 'O';

    public bool IsFuture => CategoryCode == 'F';

    public CFI(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length != 6 || !IsAllUpperAsciiLetters(value))
            throw new ArgumentException("Value must be exactly 6 uppercase ASCII letters (A-Z).", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private CFI() { }

    internal static CFI FromJson(string? value) => new() { Value = value! };

    private static bool IsAllUpperAsciiLetters(string value)
    {
        foreach (var character in value)
        {
            if (character < 'A' || character > 'Z')
                return false;
        }

        return true;
    }

    public static implicit operator string?(CFI? cfi) => cfi?.Value;

    public static implicit operator CFI(string value) => new(value);

    public override string ToString() => Value;
}
