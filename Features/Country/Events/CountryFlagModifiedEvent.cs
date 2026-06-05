using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CountryFlagModifiedEvent : EventBase, ICountryEvent
{
    public Alpha2 Alpha2 { get; init; } = null!;

    public CountryFlag Flag { get; init; } = null!;

    [JsonConstructor]
    private CountryFlagModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public CountryFlagModifiedEvent(UserID userId, EventDateTime eventDateTime, string reason, Alpha2 alpha2, CountryFlag flag)
        : this(Guid.NewGuid(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, alpha2, flag)
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

        if (alpha2 is null)
            messages.Add("Alpha2 is required.");

        if (flag is null)
            messages.Add("Flag is required.");

        return messages;
    }
}
