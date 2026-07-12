using System.Net.Mail;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

public sealed record EmailAddress
{
    public string Value { get; init; }

    [JsonConstructor]
    public EmailAddress(string value)
    {
        value = value?.Trim() ?? string.Empty;
        if (!MailAddress.TryCreate(value, out var parsed) || parsed.Address != value)
            throw new ArgumentException("Email address is invalid.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
}
