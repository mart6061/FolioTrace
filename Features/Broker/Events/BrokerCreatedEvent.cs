using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Broker Created Event")]
public sealed record BrokerCreatedEvent : EventBase, IBrokerEvent
{
    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;

    [EventProperty(Description = "LEI")]
    public LegalEntityIdentifier LEI { get; init; } = null!;

    [EventProperty(Description = "Commission")]
    public FeeRate Commission { get; init; } = null!;

    [EventProperty(Description = "Active")]
    public Active Active { get; init; } = false;

    [EventProperty(Description = "Approved Date Time")]
    public EventDateTime ApprovedDateTime { get; init; } = null!;

    [EventProperty(Description = "Next Review")]
    public EventDateTime NextReview { get; init; } = null!;

    [EventProperty(Description = "Notes")]
    public string Notes { get; init; } = string.Empty;

    [JsonConstructor]
    private BrokerCreatedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public BrokerCreatedEvent(UserID userId, EventDateTime eventDateTime, string reason, string name, LegalEntityIdentifier lei, FeeRate commission, Active active, EventDateTime approvedDateTime, EventDateTime nextReview, string notes)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, name, lei, commission, active, approvedDateTime, nextReview, notes)
    {
    }

    internal BrokerCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, string name, LegalEntityIdentifier lei, FeeRate commission, Active active, EventDateTime approvedDateTime, EventDateTime nextReview, string notes)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        Name = name;
        LEI = lei;
        Commission = commission;
        Active = active;
        ApprovedDateTime = approvedDateTime;
        NextReview = nextReview;
        Notes = notes ?? string.Empty;
    }

    public override string Type => nameof(BrokerCreatedEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, string? name, LegalEntityIdentifier? lei, FeeRate? commission, EventDateTime? approvedDateTime, EventDateTime? nextReview)
    {
        var messages = BrokerEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, lei);
        BrokerEventValidation.ValidateName(messages, name);
        BrokerEventValidation.ValidateCommission(messages, commission);
        BrokerEventValidation.ValidateApprovedDateTime(messages, approvedDateTime);
        BrokerEventValidation.ValidateNextReview(messages, nextReview);
        return messages;
    }
}
