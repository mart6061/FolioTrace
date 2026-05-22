using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FXRateSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    CurrencyPair Pair,
    FXPrice Price) : IEventRequest;
