using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIncomeSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    IInstrumentIncome Income) : IEventRequest;
