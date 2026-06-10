using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Broker Next Review Set Event")]
public sealed record BrokerNextReviewSetEvent : EventBase, IBrokerEvent
{
    [EventProperty(Description = "LEI")]
    public LegalEntityIdentifier LEI { get; init; } = null!;

    [EventProperty(Description = "Next Review")]
    public EventDateTime NextReview { get; init; } = null!;

    [JsonConstructor]
    private BrokerNextReviewSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public BrokerNextReviewSetEvent(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, EventDateTime nextReview)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, lei, nextReview)
    {
    }

    internal BrokerNextReviewSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, EventDateTime nextReview)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        LEI = lei;
        NextReview = nextReview;
    }

    public override string Type => nameof(BrokerNextReviewSetEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, LegalEntityIdentifier? lei, EventDateTime? nextReview)
    {
        var messages = BrokerEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, lei);
        BrokerEventValidation.ValidateNextReview(messages, nextReview);
        return messages;
    }
}
