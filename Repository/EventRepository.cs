using FolioTrace.Common;
using FolioTrace.Types;
using Marten;

namespace Repository;

public sealed class EventRepository(IDocumentSession session) : IEventRepository
{
    public IQueryable<T> Query<T>() where T : class, IEventBase => session.Query<T>();

    public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IEventBase
    {
        if (eventId is null)
            throw new ArgumentNullException(nameof(eventId));

        return session.LoadAsync<T>(eventId.Value, cancellationToken);
    }

    public async Task StoreAsync<T>(T document, CancellationToken cancellationToken = default) where T : class, IEventBase
    {
        if (document is null)
            throw new ArgumentNullException(nameof(document));

        session.Store(document);
        await session.SaveChangesAsync(cancellationToken);
    }
}
