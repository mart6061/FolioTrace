using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingActiveModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    bool Active) : IEventRequest;
