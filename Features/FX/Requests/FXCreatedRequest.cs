using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FXCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    Alpha3 BaseCurrency,
    Alpha3 QuoteCurrency,
    bool Active) : IEventRequest;
