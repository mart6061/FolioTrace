using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIdentifierSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    InstrumentIdentifier Identifier) : IEventRequest;
