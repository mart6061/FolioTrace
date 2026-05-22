using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IInstrumentValueReadModelRepository
{
    Task<InstrumentValues?> LoadAsync(EventDateTime valuationDateTime, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
