using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IFoleoTraderOrderEvent : IEventBase
{
    TicketNumber TicketNumber { get; }

    string ClOrdID { get; }
}
