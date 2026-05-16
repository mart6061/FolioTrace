using FolioTrace.Common;
using FolioTrace.Types;

namespace Repository;

public interface IEventRepository
{
    IQueryable<T> Query<T>() where T : class, IEventBase;

    Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IEventBase;

    Task StoreAsync<T>(T document, CancellationToken cancellationToken = default) where T : class, IEventBase;
}
