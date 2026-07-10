using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Account Identifier Set Event")]
public sealed record AccountIdentifierSetEvent : EventBase, IAccountEvent
{
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Identifier")]
    public InstrumentIdentifier Identifier { get; init; } = null!;
    [JsonConstructor]
    private AccountIdentifierSetEvent() : base(null!, null!, null!, null!, string.Empty) { }
    internal AccountIdentifierSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, InstrumentIdentifier identifier) : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        AccountID = accountID;
        Identifier = identifier;
    }
    public override string Type => nameof(AccountIdentifierSetEvent);
}
