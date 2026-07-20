using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Currency Modified Event")]
public sealed record CurrencyModifiedEvent : EventBase, ICurrencyEvent
{
    [EventProperty(Description = "Alphabetic Code")]
    public Alpha3 AlphabeticCode { get; init; } = null!;

    [EventProperty(Description = "Numeric Code")]
    public int NumericCode { get; init; }

    [EventProperty(Description = "Decimal Place")]
    public short DecimalPlace { get; init; }

    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;

    [JsonConstructor]
    private CurrencyModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public CurrencyModifiedEvent(UserID userId, EventDateTime eventDateTime, string reason, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, alphabeticCode, numericCode, decimalPlace, name)
    {
    }

    internal CurrencyModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AlphabeticCode = alphabeticCode;
        NumericCode = numericCode;
        DecimalPlace = decimalPlace;
        Name = name;
    }

    public override string Type => nameof(CurrencyModifiedEvent); // TODO: Remind me to create a universal constant for this event type.

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, Alpha3? alphabeticCode, int numericCode, short decimalPlace, string? name)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);

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
