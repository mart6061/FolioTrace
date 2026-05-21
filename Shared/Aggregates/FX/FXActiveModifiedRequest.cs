using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FXActiveModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    CurrencyPair Pair,
    bool Active) : IEventRequest;
