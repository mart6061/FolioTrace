using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketAccountAddedEvent : TicketEventBase
{
    public AccountID AccountID { get; init; } = null!;

    [JsonConstructor]
    private TicketAccountAddedEvent() : this(null!, null!, null!, null!, string.Empty, null!, null!) { }

    internal TicketAccountAddedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, AccountID accountID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        AccountID = accountID;

    public override string Type => nameof(TicketAccountAddedEvent);
}
