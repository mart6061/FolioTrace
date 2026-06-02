using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Services;

public interface IReferenceDataService<TAggregate, out TDiagnostics>
    where TAggregate : IAggregate
{
    Task<TAggregate> Current { get; }

    Task<TAggregate> Get(EventDateTime valuationDate);

    Task<TAggregate> Get(EventDateTime valuationDate, AuditDateTime asAt);

    bool IsCached(EventDateTime valuationDate);

    int InvalidateAll();

    TDiagnostics GetDiagnostics();
}
