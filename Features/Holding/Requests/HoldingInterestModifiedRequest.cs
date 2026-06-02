using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingInterestModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;
