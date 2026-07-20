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
    BrokerService brokerService,
    InstrumentService instrumentService,
    HoldingService holdingService,
    FoleoTraderOrderService foleoTraderOrderService,
    AggregateCacheInvalidationService cacheInvalidationService,
    IOptions<FoleoTraderConnectionOptions> options,
    ILogger<FoleoTraderOrderProcessor> logger)
{
    private readonly FoleoTraderConnectionOptions options = options.Value;

    public async Task<Result<FoleoTraderOrderSubmittedEvent>> SubmitOrderAsync(FoleoTraderOrderRequest request, FoleoTraderFixClient fixClient, CancellationToken cancellationToken)
    {
        var asAt = AuditDateTimeBuilder.Create();
        var ticketsTask = ticketService.Get(request.EventDateTime, asAt);
        var instrumentsTask = instrumentService.Get(request.EventDateTime, asAt);
        var brokersTask = brokerService.Get(request.EventDateTime, asAt);
        var existingOrdersTask = foleoTraderOrderService.Get(request.EventDateTime, asAt);
        await Task.WhenAll(ticketsTask, instrumentsTask, brokersTask, existingOrdersTask);
        var tickets = await ticketsTask;
        var instruments = await instrumentsTask;
        var brokers = await brokersTask;
        var existingOrders = await existingOrdersTask;
        var validation = foleoTraderOrderService.ValidateOrder(request, tickets, instruments, existingOrders);
        var validationErrors = validation.ValidationErrors.ToList();
        var executionResult = TicketTradeExecutionEventBuilder.Request(new TicketTradeExecutionRequest(request.UserID, request.EventDateTime, $"Request FIX execution for ticket {request.TicketNumber.Value}", request.TicketNumber, TradeMethodType.FIX, request.BrokerLEI), tickets, brokers);
        if (!executionResult.IsValid) validationErrors.AddRange(executionResult.ValidationErrors);

        if (validationErrors.Count > 0 || validation.Ticket is null || validation.Instrument is null)
            return Result<FoleoTraderOrderSubmittedEvent>.Invalid(validationErrors);
        var ticket = validation.Ticket;
        var fixMethod = brokers.Items.Single(item => item.LEI == request.BrokerLEI).TradeMethods.OfType<FIXTradeMethod>().Single();

        var proposalQuantity = FoleoTraderOrderService.ProposalQuantity(ticket);
        var proposalPrice = ticket.ProposalTargetPrice!;
        var clOrdID = $"FT-{ticket.TicketNumber.Value}-{Guid.CreateGuid7():N}";
        var submitted = new FoleoTraderOrderSubmittedEvent(
            Guid.CreateGuid7(),
            request.UserID,
            request.EventDateTime,
            AuditDateTimeBuilder.Create(),
            $"Send ticket {ticket.TicketNumber.Value} to FoleoTrader",
            ticket.TicketNumber,
            request.BrokerLEI,
            clOrdID,
            ticket.Side,
            proposalQuantity,
            proposalPrice,
            ticket.TradeCurrency,
            validation.SecurityID,
            validation.SecurityIDSource,
            validation.Symbol);

        await eventRepository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>>
        {
            [Constants.Initialisation.FoleoTraderOrdersStreamId] = [submitted],
            [Constants.Initialisation.TicketsStreamId] = [(IAuditEventBase)executionResult.Value!]
        }, cancellationToken: cancellationToken);
        cacheInvalidationService.Invalidate([submitted, (IAuditEventBase)executionResult.Value!]);

        try
        {
            await fixClient.SendAsync(new FoleoTraderFixOrder(
                ticket.TicketNumber.Value,
                clOrdID,
                ticket.Side,
                proposalQuantity,
                proposalPrice.Amount,
                ticket.TradeCurrency.Value,
                validation.SecurityID,
                validation.SecurityIDSource,
                validation.Symbol),
                fixMethod,
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

            var ticketFailed = new TicketTradeExecutionFailedEvent(Guid.CreateGuid7(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), $"FIX execution failed for ticket {ticket.TicketNumber.Value}", ticket.TicketNumber, TradeMethodType.FIX, request.BrokerLEI, null, exception.Message);
            await eventRepository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>> { [Constants.Initialisation.FoleoTraderOrdersStreamId] = [failed], [Constants.Initialisation.TicketsStreamId] = [ticketFailed] }, cancellationToken: cancellationToken);
            cacheInvalidationService.Invalidate([failed, ticketFailed]);
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
            if (events.OfType<FoleoTraderExecutionReceivedEvent>().Any(@event => string.Equals(@event.ExecID, report.ExecID, StringComparison.Ordinal)))
                return;

            var submittedOrder = events
                .OfType<FoleoTraderOrderSubmittedEvent>()
                .Where(@event => string.Equals(@event.ClOrdID, report.ClOrdID, StringComparison.Ordinal))
                .OrderBy(@event => @event.EventDateTime.Value)
                .ThenBy(@event => @event.AuditDateTime.Value)
                .ThenBy(@event => @event.EventID.Value)
                .LastOrDefault();

            TicketNumber ticketNumber;
            EventDateTime eventDateTime;
            if (submittedOrder is not null)
            {
                ticketNumber = submittedOrder.TicketNumber;
                eventDateTime = submittedOrder.EventDateTime;
            }
            else if (TryReadTicketNumberFromClOrdID(report.ClOrdID, out var recoveredTicketNumber))
            {
                ticketNumber = recoveredTicketNumber;
                eventDateTime = EventDateTimeBuilder.Create(DateTime.UtcNow);
                logger.LogInformation("Recovered FoleoTrader execution {ExecID} for ticket {TicketNumber} from ClOrdID {ClOrdID}.", report.ExecID, ticketNumber.Value, report.ClOrdID);
            }
            else
            {
                logger.LogWarning("Received FoleoTrader execution for unknown ClOrdID {ClOrdID}.", report.ClOrdID);
                return;
            }

            if (report.LastQty <= 0m)
                return;
            if (!FoleoTraderFillAllocator.IsWholeQuantity(report.LastQty))
            {
                logger.LogWarning("Rejected FoleoTrader fill {ExecID}: quantity {Quantity} is not a positive whole number.", report.ExecID, report.LastQty);
                return;
            }

            var asAt = AuditDateTimeBuilder.Create();
            var ticketsTask = ticketService.Get(eventDateTime, asAt);
            var holdingsTask = holdingService.Get(eventDateTime, asAt);
            var instrumentsTask = instrumentService.Get(eventDateTime, asAt);
            await Task.WhenAll(ticketsTask, holdingsTask, instrumentsTask);
            var tickets = await ticketsTask;
            var holdings = await holdingsTask;
            var instruments = await instrumentsTask;
            var ticket = tickets.Find(ticketNumber);
            if (ticket is null)
            {
                logger.LogWarning("Rejected FoleoTrader fill {ExecID}: no matching ticket {TicketNumber}.", report.ExecID, ticketNumber.Value);
                return;
            }

            var fillID = Guid.CreateGuid7();
            var brokerLEI = submittedOrder?.BrokerLEI ?? new LegalEntityIdentifier(options.BrokerLEI);
            var settlementAmount = report.GrossTradeAmt > 0m ? Round(report.GrossTradeAmt) : Round(report.LastQty * report.LastPx);
            var execution = new FoleoTraderExecutionReceivedEvent(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                AuditDateTimeBuilder.Create(),
                $"Receive FoleoTrader execution {report.ExecID} for ticket {ticketNumber.Value}",
                ticketNumber,
                report.ClOrdID,
                report.ExecID,
                fillID,
                new Price(report.LastPx),
                report.LastQty,
                new TransactionBookCost(settlementAmount),
                report.CumQty,
                report.LeavesQty,
                report.OrdStatus);

            var tradeResult = foleoTraderOrderService.CreateProratedTradeEvent(report.ExecID, report.LastQty, ticketNumber, eventDateTime, ticket, tickets, holdings, instruments, settlementAmount);
            var fillResult = TicketEventBuilder.AddFill(new TicketTradeFillRequest(
                Constants.Initialisation.UserID,
                eventDateTime,
                $"FoleoTrader FIX fill {report.ExecID}",
                ticketNumber,
                fillID,
                brokerLEI,
                new Price(report.LastPx),
                report.LastQty,
                new TransactionLocalCost(settlementAmount),
                $"FoleoTrader FIX ExecID {report.ExecID}"),
                tickets,
                allowExecutionLocked: true);

            if (!fillResult.IsValid || fillResult.Value is null)
            {
                logger.LogWarning("Rejected FoleoTrader fill {ExecID}: {ValidationErrors}.", report.ExecID, string.Join(" ", fillResult.ValidationErrors));
                return;
            }

            if (tradeResult.ValidationErrors.Count > 0)
                logger.LogWarning("Rejected FoleoTrader trade allocation {ExecID}: {ValidationErrors}.", report.ExecID, string.Join(" ", tradeResult.ValidationErrors));

            var inProgress = new TicketTradeExecutionInProgressEvent(Guid.CreateGuid7(), Constants.Initialisation.UserID, eventDateTime, AuditDateTimeBuilder.Create(), $"FIX execution received for ticket {ticketNumber.Value}", ticketNumber, TradeMethodType.FIX, brokerLEI, null);
            var ticketEvents = new List<IAuditEventBase>();
            if (tradeResult.TradeEvent is not null) ticketEvents.Add(tradeResult.TradeEvent);
            ticketEvents.Add(fillResult.Value);
            ticketEvents.Add(inProgress);
            await eventRepository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>> { [Constants.Initialisation.FoleoTraderOrdersStreamId] = [execution], [Constants.Initialisation.TicketsStreamId] = ticketEvents }, cancellationToken: cancellationToken);

            var invalidatedEvents = tradeResult.TradeEvent is null
                ? [fillResult.Value, inProgress, execution]
                : new IEventBase[] { tradeResult.TradeEvent, fillResult.Value, inProgress, execution };
            cacheInvalidationService.Invalidate(invalidatedEvents);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process FoleoTrader execution {ExecID}.", report.ExecID);
        }
    }

    private static bool TryReadTicketNumberFromClOrdID(string clOrdID, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TicketNumber? ticketNumber)
    {
        ticketNumber = null;

        if (string.IsNullOrWhiteSpace(clOrdID) || !clOrdID.StartsWith("FT-", StringComparison.Ordinal))
            return false;

        var secondSeparator = clOrdID.IndexOf('-', 3);
        if (secondSeparator <= 3)
            return false;

        if (!int.TryParse(clOrdID[3..secondSeparator], out var value))
            return false;

        try
        {
            ticketNumber = new TicketNumber(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static decimal Round(decimal value) =>
        decimal.Round(value, 8, MidpointRounding.AwayFromZero);
}
