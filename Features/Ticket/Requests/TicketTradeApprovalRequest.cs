using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeApprovalRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber) : IEventRequest;
