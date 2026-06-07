using FolioTrace.Types;
using Repository;

namespace FolioTrace.Aggregates;

public sealed class FoleoTraderOrderService(IEventRepository eventRepository)
{
    public int Invalidate(IFoleoTraderOrderEvent @event) => 0;

    public async Task<FoleoTraderOrders> Get(EventDateTime valuationDate, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.LoadStreamAsync<IFoleoTraderOrderEvent>(Constants.Initialisation.FoleoTraderOrdersStreamId, cancellationToken);
        return new FoleoTraderOrders(valuationDate, events);
    }

    public async Task<FoleoTraderOrders> Get(EventDateTime valuationDate, AuditDateTime asAt, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.LoadStreamAsync<IFoleoTraderOrderEvent>(Constants.Initialisation.FoleoTraderOrdersStreamId, cancellationToken);
        return new FoleoTraderOrders(valuationDate, asAt, events);
    }
}
