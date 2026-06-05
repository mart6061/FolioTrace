using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CurrencyCreatedEvent : EventBase, ICurrencyEvent
{
    public Alpha3 AlphabeticCode { get; init; } = null!;

    public int NumericCode { get; init; }

    public short DecimalPlace { get; init; }

    public string Name { get; init; } = string.Empty;

    [JsonConstructor]
    private CurrencyCreatedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public CurrencyCreatedEvent(UserID userId, EventDateTime eventDateTime, string reason, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
        : this(Guid.NewGuid(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, alphabeticCode, numericCode, decimalPlace, name)
    {
    }

    internal CurrencyCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AlphabeticCode = alphabeticCode;
        NumericCode = numericCode;
        DecimalPlace = decimalPlace;
        Name = name;
    }

    public override string Type => nameof(CurrencyCreatedEvent); // TODO: Remind me to create a universal constant for this event type.

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, Alpha3? alphabeticCode, int numericCode, short decimalPlace, string? name)
    {
        var messages = new List<string>();

        if (eventId is null)
            messages.Add("EventID is required.");

        if (userId is null)
            messages.Add("UserID is required.");

        if (eventDateTime is null)
            messages.Add("EventDateTime is required.");

        if (auditDateTime is null)
            messages.Add("AuditDateTime is required.");

        if (string.IsNullOrWhiteSpace(reason))
            messages.Add("Reason is required.");

        if (alphabeticCode is null)
            messages.Add("AlphabeticCode is required.");

        if (numericCode < 0 || numericCode > 999)
            messages.Add("NumericCode must be between 0 and 999.");

        if (decimalPlace < 0)
            messages.Add("DecimalPlace must be zero or greater.");

        if (string.IsNullOrWhiteSpace(name))
            messages.Add("Name is required.");

        return messages;
    }
}
