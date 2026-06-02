using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IHoldingCreatedRequest : IEventRequest
{
    HoldingID? HoldingID { get; }
    AccountID AccountID { get; }
    InstrumentID InstrumentID { get; }
    string Name { get; }
    bool Active { get; }
    bool Default { get; }
}
