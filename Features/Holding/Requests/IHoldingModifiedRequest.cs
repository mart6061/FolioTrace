using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IHoldingModifiedRequest : IEventRequest
{
    HoldingID HoldingID { get; }
    string Name { get; }
    bool Default { get; }
}
