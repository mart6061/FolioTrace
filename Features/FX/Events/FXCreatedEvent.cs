using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "FX Created Event")]
public sealed record FXCreatedEvent : EventBase, IFXEvent
{
    [EventProperty(Description = "Pair")]
    public CurrencyPair Pair { get; init; } = null!;

    [EventProperty(Description = "Base Currency")]
    public Alpha3 BaseCurrency { get; init; } = null!;

    [EventProperty(Description = "Quote Currency")]
    public Alpha3 QuoteCurrency { get; init; } = null!;

    [EventProperty(Description = "Active")]
    public Active Active { get; init; } = false;

    [JsonConstructor]
    private FXCreatedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal FXCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, CurrencyPair pair, Active active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        Pair = pair;
        BaseCurrency = pair.BaseCurrency;
        QuoteCurrency = pair.QuoteCurrency;
        Active = active;
    }

    public override string Type => nameof(FXCreatedEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, CurrencyPair? pair)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);

        if (pair is null)
            messages.Add("Pair is required.");

        return messages;
    }
}
