using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountDisplayOrderSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    AccountID AccountID,
    DisplayOrder DisplayOrder) : IEventRequest;
