using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Account Identifier Unset Event")]
public sealed record AccountIdentifierUnsetEvent : EventBase, IAccountEvent
{
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Identifier Type")]
    public InstrumentIdentifierType IdentifierType { get; init; }
    [JsonConstructor]
    private AccountIdentifierUnsetEvent() : base(null!, null!, null!, null!, string.Empty) { }
    internal AccountIdentifierUnsetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, InstrumentIdentifierType identifierType) : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        AccountID = accountID;
        IdentifierType = identifierType;
    }
    public override string Type => nameof(AccountIdentifierUnsetEvent);
}
