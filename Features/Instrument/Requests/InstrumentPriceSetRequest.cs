using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentPriceSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    IInstrumentPrice Price) : IEventRequest;
