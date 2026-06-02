using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingInspecieInModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;
