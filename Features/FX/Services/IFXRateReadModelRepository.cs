using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IFXRateReadModelRepository
{
    Task<FXRates?> LoadAsync(EventDateTime valuationDateTime, CancellationToken cancellationToken = default);

    Task RebuildAsync(CancellationToken cancellationToken = default);

    Task RebuildPairAsync(CurrencyPair pair, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
