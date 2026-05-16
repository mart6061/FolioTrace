using FolioTrace.Common;
using FolioTrace.Types;

namespace Repository;

public interface IEventRepository
{
    Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IEventBase;

    Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IEventBase;
}
