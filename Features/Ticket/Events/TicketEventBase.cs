using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record TicketEventBase(EventID EventID, UserID UserID, EventDateTime EventDateTime, AuditDateTime AuditDateTime, string Reason, TicketNumber TicketNumber)
    : EventBase(EventID, UserID, EventDateTime, AuditDateTime, Reason), ITicket;
