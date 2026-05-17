using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

// Marker interface for domain models
public interface IAggregate : IType
{
    EventID LastEventID { get; }

    LastAuditDateTime LastAuditDateTime { get; }

    EventDateTime ValuationDateTime { get; }

    AuditDateTime AsOfDateTime { get; }
}
