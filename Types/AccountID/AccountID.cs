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

    public string ToData() => Value.ToString();

    public string ToDetail() => $"{nameof(AccountID)}: {this}";
}

internal sealed class AccountIDJsonConverter : JsonConverter<AccountID>
{
    public override AccountID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for AccountID value.");

        return AccountID.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, AccountID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
