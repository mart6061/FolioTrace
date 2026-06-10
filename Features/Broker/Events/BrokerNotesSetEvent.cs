using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Broker Notes Set Event")]
public sealed record BrokerNotesSetEvent : EventBase, IBrokerEvent
{
    [EventProperty(Description = "LEI")]
    public LegalEntityIdentifier LEI { get; init; } = null!;

    [EventProperty(Description = "Notes")]
    public string Notes { get; init; } = string.Empty;

    [JsonConstructor]
    private BrokerNotesSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public BrokerNotesSetEvent(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, string notes)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, lei, notes)
    {
    }

    internal BrokerNotesSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, string notes)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        LEI = lei;
        Notes = notes ?? string.Empty;
    }

    public override string Type => nameof(BrokerNotesSetEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, LegalEntityIdentifier? lei) =>
        BrokerEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, lei);
}
