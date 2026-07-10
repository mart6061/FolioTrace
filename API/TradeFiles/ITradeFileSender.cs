using FolioTrace.Aggregates;

namespace API.TradeFiles;

public interface ITradeFileSender
{
    bool Supports(ITradeMethodFileSendConfig configuration);
    Task SendAsync(TradeFileDeliveryRequest request, CancellationToken cancellationToken);
}
