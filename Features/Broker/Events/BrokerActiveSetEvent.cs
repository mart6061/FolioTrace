using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerActiveSetEvent : EventBase, IBrokerEvent
{
    public LegalEntityIdentifier LEI { get; init; } = null!;

    public Active Active { get; init; } = false;

    [JsonConstructor]
    private BrokerActiveSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public BrokerActiveSetEvent(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, Active active)
        : this(Guid.NewGuid(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, lei, active)
    {
    }

    internal BrokerActiveSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, Active active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        LEI = lei;
        Active = active;
    }

    public override string Type => nameof(BrokerActiveSetEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, LegalEntityIdentifier? lei) =>
        BrokerEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, lei);
}
