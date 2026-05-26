using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    HoldingNominalType? NominalType,
    string Name,
    bool Default) : IEventRequest;
