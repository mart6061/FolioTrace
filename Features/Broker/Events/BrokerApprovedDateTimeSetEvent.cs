using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Broker Approved Date Time Set Event")]
public sealed record BrokerApprovedDateTimeSetEvent : EventBase, IBrokerEvent
{
    [EventProperty(Description = "LEI")]
    public LegalEntityIdentifier LEI { get; init; } = null!;

    [EventProperty(Description = "Approved Date Time")]
    public EventDateTime ApprovedDateTime { get; init; } = null!;

    [JsonConstructor]
    private BrokerApprovedDateTimeSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public BrokerApprovedDateTimeSetEvent(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, EventDateTime approvedDateTime)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, lei, approvedDateTime)
    {
    }

    internal BrokerApprovedDateTimeSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, EventDateTime approvedDateTime)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        LEI = lei;
        ApprovedDateTime = approvedDateTime;
    }

    public override string Type => nameof(BrokerApprovedDateTimeSetEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, LegalEntityIdentifier? lei, EventDateTime? approvedDateTime)
    {
        var messages = BrokerEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, lei);
        BrokerEventValidation.ValidateApprovedDateTime(messages, approvedDateTime);
        return messages;
    }
}
