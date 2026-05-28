using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(BankAccountNumberJsonConverter))]
public sealed record BankAccountNumber : IType
{
    public string Value { get; init; } = null!;

    public BankAccountNumber(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        value = value.Trim();
        if (value.Length is < 6 or > 12 || !value.All(IsDigit))
            throw new ArgumentException("Value must contain 6 to 12 digits.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private BankAccountNumber() { }

    internal static BankAccountNumber FromJson(string? value) => new(value!);

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    public static implicit operator string?(BankAccountNumber? accountNumber) => accountNumber?.Value;

    public static implicit operator BankAccountNumber(string s) => new(s);

    public override string ToString() => Value;

    public string ToData() => Value;

    public string ToDetail() => $"{nameof(BankAccountNumber)}: {this}";
}

internal sealed class BankAccountNumberJsonConverter : JsonConverter<BankAccountNumber>
{
    public override BankAccountNumber? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for BankAccountNumber value.");

        return BankAccountNumber.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, BankAccountNumber value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
