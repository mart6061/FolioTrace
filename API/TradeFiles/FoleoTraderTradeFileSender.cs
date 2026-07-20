using System.Net.Http.Headers;
using System.Net.Http.Json;
using FolioTrace.Aggregates;
using Microsoft.Extensions.Options;

namespace API.TradeFiles;

public sealed class FoleoTraderTradeFileSender(HttpClient httpClient, IOptions<TradeFileOptions> options) : ITradeFileSender
{
    public bool Supports(ITradeMethodFileSendConfig configuration) => configuration is FTPTradeMethodFileSendConfig;

    public async Task SendAsync(TradeFileDeliveryRequest request, CancellationToken cancellationToken)
    {
        await using (request)
        using (var form = new MultipartFormDataContent())
        {
            var metadata = new TradeFileDeliveryMetadata(
                request.TradeFileID,
                request.BrokerLEI,
                request.FileName,
                request.MediaType,
                request.ContentLength,
                request.AcknowledgementUrl,
                request.ConfirmationUrl,
                request.Tickets);
            form.Add(JsonContent.Create(metadata), "metadata");
            var file = new StreamContent(request.Content);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse(request.MediaType);
            form.Add(file, "file", request.FileName);

            using var response = await httpClient.PostAsync(options.Value.FoleoTraderReceiverUrl, form, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}
