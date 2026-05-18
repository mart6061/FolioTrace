using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IModel : IType
{
    EventID LastEventID { get; }

    LastAuditDateTime LastAuditDateTime { get; }

    EventDateTime ValuationDateTime { get; }

    AuditDateTime AsOfDateTime { get; }
}
