using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerModifiedEvent : EventBase, IBrokerEvent
{
    public LegalEntityIdentifier LEI { get; init; } = null!;

    public string Name { get; init; } = string.Empty;

    public FeeRate Commission { get; init; } = null!;

    [JsonConstructor]
    private BrokerModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public BrokerModifiedEvent(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, string name, FeeRate commission)
        : this(Guid.NewGuid(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, lei, name, commission)
    {
    }

    internal BrokerModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, string name, FeeRate commission)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        LEI = lei;
        Name = name;
        Commission = commission;
    }

    public override string Type => nameof(BrokerModifiedEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, LegalEntityIdentifier? lei, string? name, FeeRate? commission)
    {
        var messages = BrokerEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, lei);
        BrokerEventValidation.ValidateName(messages, name);
        BrokerEventValidation.ValidateCommission(messages, commission);
        return messages;
    }
}
