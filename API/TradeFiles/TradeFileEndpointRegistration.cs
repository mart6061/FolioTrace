using System.Security.Cryptography;
using System.Text;
using FolioTrace.Aggregates;
using FolioTrace;
using FolioTrace.Types;
using Microsoft.Extensions.Options;
using Repository;
using Services;

namespace API.TradeFiles;

public static class TradeFileEndpointRegistration
{
    public static RouteGroupBuilder MapTradeFileCallbackEndpoints(this RouteGroupBuilder api)
    {
        var callbacks = api.MapGroup("/TradeFiles").WithTags("TradeFiles").AllowAnonymous();
        callbacks.MapPost("/Acknowledgements", async (HttpContext context, TradeFileReceivedConfirm confirmation, TradeFileWorkflowService workflow, IOptions<TradeFileOptions> options, CancellationToken cancellationToken) =>
            !IsAuthorizedCallback(context, options.Value)
                ? Results.Unauthorized()
                : await Execute(() => workflow.AcknowledgeAsync(confirmation, cancellationToken)));
        callbacks.MapPost("/Confirmations", async (HttpContext context, TradeFileTradeConfirm confirmation, TradeFileWorkflowService workflow, IOptions<TradeFileOptions> options, CancellationToken cancellationToken) =>
            !IsAuthorizedCallback(context, options.Value)
                ? Results.Unauthorized()
                : await Execute(() => workflow.ConfirmAsync(confirmation, cancellationToken)));
        return api;
    }

    private static bool IsAuthorizedCallback(HttpContext context, TradeFileOptions options)
    {
        if (string.IsNullOrEmpty(options.CallbackSecret))
            return true;

        var provided = context.Request.Headers[TradeFileOptions.CallbackSecretHeaderName].ToString();
        if (string.IsNullOrEmpty(provided))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(options.CallbackSecret));
    }

    public static RouteGroupBuilder MapTradeFileEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/TradeFiles").WithTags("TradeFiles");
        group.MapGet("/", async (TradeFileService service, CancellationToken cancellationToken) =>
        {
            var aggregate = await service.Get(ReferenceDataCurrent.EndOfToday(), AuditDateTimeBuilder.Create(), cancellationToken);
            return Results.Ok(aggregate.Items);
        });
        group.MapGet("/Active", async (TradeFileService service, CancellationToken cancellationToken) =>
        {
            var aggregate = await service.Get(ReferenceDataCurrent.EndOfToday(), AuditDateTimeBuilder.Create(), cancellationToken);
            return Results.Ok(aggregate.Items.Where(item => item.Status is not TradeFileStatus.Completed and not TradeFileStatus.Failed));
        });
        group.MapPost("/Requests", async (TradeFileRequest request, TradeFileWorkflowService workflow, CancellationToken cancellationToken) =>
            await Execute(async () => Results.Accepted(value: new { TradeFileID = (await workflow.RequestAsync(request, cancellationToken)).Value })));
        group.MapPost("/Pending", async (TicketTradeExecutionRequest request, TicketService ticketService, BrokerService brokerService, IEventRepository repository, AggregateCacheInvalidationService cacheInvalidationService, CancellationToken cancellationToken) =>
        {
            if (request.TradeMethodType != TradeMethodType.TradeFile)
                return Results.BadRequest(new { error = "TradeFile method is required." });
            var asAt = AuditDateTimeBuilder.Create();
            var ticketsTask = ticketService.Get(request.EventDateTime, asAt);
            var brokersTask = brokerService.Get(request.EventDateTime, asAt);
            await Task.WhenAll(ticketsTask, brokersTask);
            var tickets = await ticketsTask;
            var brokers = await brokersTask;
            var result = TicketTradeExecutionEventBuilder.Request(request, tickets, brokers);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);
            await repository.AppendAsync(Constants.Initialisation.TicketsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Ok(new { EventID = result.Value.EventID.Value });
        });
        group.MapGet("/{tradeFileID:guid}/File", async (Guid tradeFileID, TradeFileService service, IStoredFileRepository storedFileRepository, CancellationToken cancellationToken) =>
        {
            var aggregate = await service.Get(ReferenceDataCurrent.EndOfToday(), AuditDateTimeBuilder.Create(), cancellationToken);
            var tradeFile = aggregate.Find(new TradeFileID(tradeFileID));
            if (tradeFile?.StoredFileID is null) return Results.NotFound();
            return (IResult)new StoredFileHttpResult(tradeFile.StoredFileID.Value, storedFileRepository);
        }).WithMetadata(new DisableRequestTraceBodyCaptureAttribute());
        return api;
    }

    private static async Task<IResult> Execute(Func<Task> action)
    {
        try { await action(); return Results.Ok(); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }
}
