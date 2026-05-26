using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    HoldingType HoldingType,
    HoldingNominalType? NominalType,
    string Name,
    bool Active,
    bool Default) : IEventRequest;
