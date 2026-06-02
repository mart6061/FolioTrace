using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID? InstrumentID,
    string Name,
    string FormalName,
    Exchange Exchange,
    CFI CFI,
    InstrumentLogo? Logo,
    bool Active,
    Alpha2 IncomeCountry,
    Alpha2 PriceCountry,
    Alpha3 PriceCurrency) : IEventRequest;
