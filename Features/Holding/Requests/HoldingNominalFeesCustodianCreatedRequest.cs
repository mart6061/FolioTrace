using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingNominalFeesCustodianCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;
