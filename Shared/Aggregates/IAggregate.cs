using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IAggregate : IType
{
    EventID LastEventID { get; }

    LastAuditDateTime LastAuditDateTime { get; }

    EventDateTime ValuationDateTime { get; }

    AuditDateTime AsOfDateTime { get; }
}
