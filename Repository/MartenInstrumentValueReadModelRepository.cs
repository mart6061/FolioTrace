using FolioTrace.Aggregates;
using FolioTrace.Types;
using Marten;

namespace Repository;

public sealed class MartenInstrumentValueReadModelRepository(IDocumentStore store) : IInstrumentValueReadModelRepository
{
    public Task<InstrumentValues?> LoadAsync(EventDateTime valuationDateTime, CancellationToken cancellationToken = default) =>
        Task.FromResult<InstrumentValues?>(null);

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var session = store.LightweightSession();
        session.DeleteWhere<InstrumentDefinitionReadModel>(_ => true);
        session.DeleteWhere<InstrumentPricePointReadModel>(_ => true);
        session.DeleteWhere<InstrumentIncomePointReadModel>(_ => true);
        await session.SaveChangesAsync(cancellationToken);
    }
}
