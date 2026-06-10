using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountActiveSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    AccountID AccountID,
    bool Active) : IEventRequest;
