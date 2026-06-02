using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingIncomeModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;
