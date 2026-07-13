using FolioTrace.Types;
using Repository;
namespace FolioTrace.Aggregates;
public sealed class TradeFileService(IEventRepository eventRepository)
{
    public async Task<TradeFiles> Get(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, CancellationToken cancellationToken = default) =>
        new(valuationDateTime, asOfDateTime, await eventRepository.LoadStreamAsync<ITradeFileEvent>(Constants.Initialisation.TradeFilesStreamId, cancellationToken));
}
