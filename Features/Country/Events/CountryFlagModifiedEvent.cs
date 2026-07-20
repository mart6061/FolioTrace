using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Country Flag Modified Event")]
public sealed record CountryFlagModifiedEvent : EventBase, ICountryEvent
{
    [EventProperty(Description = "Alpha2")]
    public Alpha2 Alpha2 { get; init; } = null!;

    [EventProperty(Description = "Flag")]
    public CountryFlag Flag { get; init; } = null!;

    [JsonConstructor]
    private CountryFlagModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public CountryFlagModifiedEvent(UserID userId, EventDateTime eventDateTime, string reason, Alpha2 alpha2, CountryFlag flag)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, alpha2, flag)
    {
    }

    internal CountryFlagModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha2 alpha2, CountryFlag flag)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        Alpha2 = alpha2;
        Flag = flag;
    }

    public override string Type => nameof(CountryFlagModifiedEvent); // TODO: Remind me to create a universal constant for this event type.

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, Alpha2? alpha2, CountryFlag? flag)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);

        if (alpha2 is null)
            messages.Add("Alpha2 is required.");

        if (flag is null)
            messages.Add("Flag is required.");

        return messages;
    }
}
