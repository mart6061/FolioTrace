using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingCreatedEvent : EventBase, IHoldingEvent
{
    public HoldingID HoldingID { get; init; } = null!;
    public AccountID AccountID { get; init; } = null!;
    public InstrumentID InstrumentID { get; init; } = null!;
    public HoldingType HoldingType { get; init; }
    public HoldingNominalType? NominalType { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Active { get; init; }
    public bool Default { get; init; }

    [JsonConstructor]
    private HoldingCreatedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal HoldingCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, HoldingType holdingType, HoldingNominalType? nominalType, string name, bool active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        HoldingID = holdingID;
        AccountID = accountID;
        InstrumentID = instrumentID;
        HoldingType = holdingType;
        NominalType = nominalType;
        Name = name.Trim();
        Active = active;
        Default = isDefault;
    }

    public override string Type => nameof(HoldingCreatedEvent);

    public static List<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, HoldingID? holdingID, AccountID? accountID, InstrumentID? instrumentID, HoldingType holdingType, HoldingNominalType? nominalType, string? name, bool isDefault)
    {
        var messages = HoldingEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, holdingID);
        HoldingEventValidation.ValidateReferences(messages, accountID, instrumentID, null, null);
        HoldingEventValidation.ValidateDefinition(messages, holdingType, nominalType, name, isDefault);
        return messages;
    }
}
