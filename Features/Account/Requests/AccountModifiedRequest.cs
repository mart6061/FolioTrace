using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    AccountID AccountID,
    string Name,
    string FormalName) : IEventRequest;
