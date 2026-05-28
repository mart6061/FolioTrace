using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IHoldingEvent : IEventBase
{
    HoldingID HoldingID { get; }
}
