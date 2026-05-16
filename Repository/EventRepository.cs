using FolioTrace.Common;
using FolioTrace.Types;
using Marten;

namespace Repository;

public sealed class EventRepository(IDocumentSession session) : IEventRepository
{
    public async Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IEventBase
    {
        if (eventId is null)
            throw new ArgumentNullException(nameof(eventId));

        var @event = await session.Events.LoadAsync<T>(eventId.Value, cancellationToken);
        return @event?.Data;
    }

    public async Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IEventBase
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        session.Events.Append(streamId, @event);
        await session.SaveChangesAsync(cancellationToken);
    }
}
