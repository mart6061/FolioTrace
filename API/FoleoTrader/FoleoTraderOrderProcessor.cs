using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using FolioTrace;
using Microsoft.Extensions.Options;
using Repository;
using Services;

namespace API.FoleoTrader;

public sealed class FoleoTraderOrderProcessor(
    IEventRepository eventRepository,
    TicketService ticketService,
    InstrumentService instrumentService,
    FoleoTraderOrderService foleoTraderOrderService,
    AggregateCacheInvalidationService cacheInvalidationService,
    IOptions<FoleoTraderOptions> options,
    ILogger<FoleoTraderOrderProcessor> logger)
{
    private readonly FoleoTraderOptions options = options.Value;

    public async Task<Result<FoleoTraderOrderSubmittedEvent>> SubmitOrderAsync(FoleoTraderOrderRequest request, FoleoTraderFixClient fixClient, CancellationToken cancellationToken)
    {
        var asAt = AuditDateTimeBuilder.Create();
        var tickets = await ticketService.Get(request.EventDateTime, asAt);
        var instruments = await instrumentService.Get(request.EventDateTime, asAt);
        var existingOrders = await foleoTraderOrderService.Get(request.EventDateTime, asAt);
        var validationErrors = ValidateOrder(request, tickets, instruments, existingOrders, out var ticket, out var instrument, out var securityID, out var securityIDSource, out var symbol);

        if (validationErrors.Count > 0 || ticket is null || instrument is null)
            return Result<FoleoTraderOrderSubmittedEvent>.Invalid(validationErrors);

        var proposalQuantity = ticket.ProposalTotalAmount!.Value;
        var proposalPrice = ticket.ProposalTargetPrice!;
        var clOrdID = $"FT-{ticket.TicketNumber.Value}-{Guid.CreateGuid7():N}";
        var submitted = new FoleoTraderOrderSubmittedEvent(
            Guid.CreateGuid7(),
            request.UserID,
            request.EventDateTime,
            AuditDateTimeBuilder.Create(),
            $"Send ticket {ticket.TicketNumber.Value} to FoleoTrader",
            ticket.TicketNumber,
            clOrdID,
            ticket.Side,
            proposalQuantity,
            proposalPrice,
            ticket.TradeCurrency,
            securityID,
            securityIDSource,
            symbol);

        await eventRepository.AppendAsync(Constants.Initialisation.FoleoTraderOrdersStreamId, submitted, cancellationToken);
        cacheInvalidationService.Invalidate(submitted);

        try
        {
            await fixClient.SendAsync(new FoleoTraderFixOrder(
                ticket.TicketNumber.Value,
                clOrdID,
                ticket.Side,
                proposalQuantity,
                proposalPrice.Amount,
                ticket.TradeCurrency.Value,
                securityID,
                securityIDSource,
                symbol),
                cancellationToken);
        }
        catch (Exception exception)
        {
            var failed = new FoleoTraderOrderFailedEvent(
                Guid.CreateGuid7(),
                request.UserID,
                request.EventDateTime,
                AuditDateTimeBuilder.Create(),
                $"FoleoTrader send failed for ticket {ticket.TicketNumber.Value}",
                ticket.TicketNumber,
                clOrdID,
                exception.Message);

            await eventRepository.AppendAsync(Constants.Initialisation.FoleoTraderOrdersStreamId, failed, cancellationToken);
            cacheInvalidationService.Invalidate(failed);
            logger.LogError(exception, "Failed to send FoleoTrader FIX order {ClOrdID}.", clOrdID);

            return Result<FoleoTraderOrderSubmittedEvent>.Invalid([exception.Message]);
        }

        return Result<FoleoTraderOrderSubmittedEvent>.Success(submitted);
    }

    public async Task ProcessExecutionReportAsync(FoleoTraderExecutionReport report, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(report.ClOrdID) || string.IsNullOrWhiteSpace(report.ExecID))
                return;

            var events = await eventRepository.LoadStreamAsync<IFoleoTraderOrderEvent>(Constants.Initialisation.FoleoTraderOrdersStreamId, cancellationToken);
            if (events.OfType<FoleoTraderExecutionReceivedEvent>().Any(@event => @event.ExecID == report.ExecID))
                return;

            var submitted = events
                .OfType<FoleoTraderOrderSubmittedEvent>()
                .LastOrDefault(@event => string.Equals(@event.ClOrdID, report.ClOrdID, StringComparison.Ordinal));
            if (submitted is null)
            var orders = new FoleoTraderOrders(EventDateTimeBuilder.Create(DateTime.UtcNow), events);
            var order = orders.FindByClOrdID(report.ClOrdID);
            if (order is null)
            {
                logger.LogWarning("Received FoleoTrader execution for unknown ClOrdID {ClOrdID}.", report.ClOrdID);
                return;
            }

            if (report.LastQty <= 0m)
                return;

            var eventDateTime = submitted.EventDateTime;
            var eventDateTime = EventDateTimeBuilder.Create(DateTime.UtcNow);
            var fillID = Guid.CreateGuid7();
            var bookCost = report.GrossTradeAmt > 0m ? report.GrossTradeAmt : decimal.Round(report.LastQty * report.LastPx, 8);
            var execution = new FoleoTraderExecutionReceivedEvent(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                AuditDateTimeBuilder.Create(),
                $"Receive FoleoTrader execution {report.ExecID} for ticket {submitted.TicketNumber.Value}",
                submitted.TicketNumber,
                $"Receive FoleoTrader execution {report.ExecID} for ticket {order.TicketNumber.Value}",
                order.TicketNumber,
                report.ClOrdID,
                report.ExecID,
                fillID,
                new Price(report.LastPx),
                report.LastQty,
                new TransactionBookCost(bookCost),
                report.CumQty,
                report.LeavesQty,
                report.OrdStatus);

            var fillResult = TicketEventBuilder.AddFill(new TicketTradeFillRequest(
                Constants.Initialisation.UserID,
                eventDateTime,
                $"FoleoTrader FIX fill {report.ExecID}",
                submitted.TicketNumber,
                order.TicketNumber,
                fillID,
                new LegalEntityIdentifier(options.BrokerLEI),
                new Price(report.LastPx),
                report.LastQty,
                new TransactionBookCost(bookCost),
                $"FoleoTrader FIX ExecID {report.ExecID}"),
                await ticketService.Get(eventDateTime, AuditDateTimeBuilder.Create()));

            if (!fillResult.IsValid || fillResult.Value is null)
            {
                logger.LogWarning("Rejected FoleoTrader fill {ExecID}: {ValidationErrors}.", report.ExecID, string.Join(" ", fillResult.ValidationErrors));
                return;
            }

            await eventRepository.AppendAsync(Constants.Initialisation.FoleoTraderOrdersStreamId, execution, cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.TicketsStreamId, fillResult.Value, cancellationToken);
            cacheInvalidationService.Invalidate(new IEventBase[] { execution, fillResult.Value });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process FoleoTrader execution {ExecID}.", report.ExecID);
        }
    }

    private static List<string> ValidateOrder(
        FoleoTraderOrderRequest request,
        Tickets tickets,
        Instruments instruments,
        FoleoTraderOrders existingOrders,
        out Ticket? ticket,
        out Instrument? instrument,
        out string securityID,
        out string securityIDSource,
        out string symbol)
    {
        var messages = new List<string>();
        ticket = tickets.Find(request.TicketNumber);
        instrument = null;
        securityID = string.Empty;
        securityIDSource = string.Empty;
        symbol = string.Empty;

        if (ticket is null)
        {
            messages.Add($"No matching ticket found for TicketNumber '{request.TicketNumber.Value}'.");
            return messages;
        }

        var ticketInstrumentID = ticket.InstrumentID;
        instrument = instruments.Items.FirstOrDefault(item => item.InstrumentID == ticketInstrumentID);
        if (instrument is null)
            messages.Add($"No matching instrument found for InstrumentID '{ticket.InstrumentID.Value}'.");
        else if (!instrument.CFI.IsEquity && !instrument.CFI.IsDebt)
            messages.Add("FoleoTrader only supports equity and fixed interest instruments.");

        if (ticket.Stage != TicketStage.Trade)
            messages.Add("Ticket must be in Trade stage.");
        if (ticket.ProposalDecision != TicketDecision.Approved || ticket.TradeDecision != TicketDecision.InProgress)
            messages.Add("Ticket must be approved for proposal and in progress for trade.");
        if (ticket.TradePrice is not null || ticket.TradeAllocations.Count > 0 || ticket.Fills.Count > 0)
            messages.Add("FoleoTrader can only be sent before trade fields or fills have been saved.");
        if (ticket.ProposalTargetPrice is null || ticket.ProposalTotalAmount is null)
            messages.Add("Proposal price and quantity are required.");
        if (existingOrders.Find(ticket.TicketNumber) is not null)
            messages.Add("Ticket has already been sent to FoleoTrader.");

        if (instrument is not null)
        {
            var isin = instrument.Identifiers.FirstOrDefault(identifier => identifier.Type == InstrumentIdentifierType.ISIN)?.Value;
            var ticker = instrument.Identifiers.FirstOrDefault(identifier => identifier.Type == InstrumentIdentifierType.Ticker)?.Value;
            securityID = !string.IsNullOrWhiteSpace(isin) ? isin : ticker ?? string.Empty;
            securityIDSource = !string.IsNullOrWhiteSpace(isin) ? "4" : "8";
            symbol = ticker ?? instrument.Name;

            if (string.IsNullOrWhiteSpace(securityID))
                messages.Add("Instrument needs an ISIN or ticker identifier before sending to FoleoTrader.");
        }

        return messages;
    }
}
