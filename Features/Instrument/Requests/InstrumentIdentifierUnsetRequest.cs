using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIdentifierUnsetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    InstrumentIdentifierType IdentifierType) : IEventRequest;
