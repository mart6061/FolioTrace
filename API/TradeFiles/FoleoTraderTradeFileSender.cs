using System.Net.Http.Json;
using FolioTrace.Aggregates;
using Microsoft.Extensions.Options;

namespace API.TradeFiles;

public sealed class FoleoTraderTradeFileSender(HttpClient httpClient, IOptions<TradeFileOptions> options) : ITradeFileSender
{
    public bool Supports(ITradeMethodFileSendConfig configuration) => configuration is FTPTradeMethodFileSendConfig;

    public async Task SendAsync(TradeFileDeliveryRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(options.Value.FoleoTraderReceiverUrl, request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
