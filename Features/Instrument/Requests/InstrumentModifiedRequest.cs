using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    string Name,
    string FormalName,
    Exchange Exchange,
    CFI CFI,
    InstrumentLogo? Logo,
    Alpha2 IncomeCountry,
    Alpha2 PriceCountry) : IEventRequest;
