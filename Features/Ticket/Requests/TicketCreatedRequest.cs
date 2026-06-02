using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketSide Side,
    InstrumentID InstrumentID);
