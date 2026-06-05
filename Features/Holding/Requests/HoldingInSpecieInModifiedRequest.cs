using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingInSpecieInModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;
