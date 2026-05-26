using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountActiveModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    AccountID AccountID,
    bool Active) : IEventRequest;
