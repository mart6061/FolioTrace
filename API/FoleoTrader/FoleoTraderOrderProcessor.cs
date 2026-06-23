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
    HoldingService holdingService,
    FoleoTraderOrderService foleoTraderOrderService,
    AggregateCacheInvalidationService cacheInvalidationService,
    IOptions<FoleoTraderOptions> options,
    ILogger<FoleoTraderOrderProcessor> logger)
{
    private const int DecimalScale = 8;
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

        var proposalQuantity = ProposalQuantity(ticket);
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
            var tickets = await ticketService.Get(eventDateTime, asAt);
            var holdings = await holdingService.Get(eventDateTime, asAt);
            var instruments = await instrumentService.Get(eventDateTime, asAt);
            var ticket = tickets.Find(ticketNumber);
            if (ticket is null)
            {
                logger.LogWarning("Rejected FoleoTrader fill {ExecID}: no matching ticket {TicketNumber}.", report.ExecID, ticketNumber.Value);
                return;
            }

            var fillID = Guid.CreateGuid7();
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

            var tradeResult = CreateProratedTradeEvent(report, ticketNumber, eventDateTime, ticket, tickets, holdings, instruments, settlementAmount);
            var fillResult = TicketEventBuilder.AddFill(new TicketTradeFillRequest(
                Constants.Initialisation.UserID,
                eventDateTime,
                $"FoleoTrader FIX fill {report.ExecID}",
                ticketNumber,
                fillID,
                new LegalEntityIdentifier(options.BrokerLEI),
                new Price(report.LastPx),
                report.LastQty,
                new TransactionLocalCost(settlementAmount),
                $"FoleoTrader FIX ExecID {report.ExecID}"),
                tickets);

            if (!fillResult.IsValid || fillResult.Value is null)
            {
                logger.LogWarning("Rejected FoleoTrader fill {ExecID}: {ValidationErrors}.", report.ExecID, string.Join(" ", fillResult.ValidationErrors));
                return;
            }

            await eventRepository.AppendAsync(Constants.Initialisation.FoleoTraderOrdersStreamId, execution, cancellationToken);
            if (tradeResult.TradeEvent is not null)
                await eventRepository.AppendAsync(Constants.Initialisation.TicketsStreamId, tradeResult.TradeEvent, cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.TicketsStreamId, fillResult.Value, cancellationToken);

            var invalidatedEvents = tradeResult.TradeEvent is null
                ? [fillResult.Value, execution]
                : new IEventBase[] { tradeResult.TradeEvent, fillResult.Value, execution };
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
        if (ticket.ProposalTargetPrice is null || ProposalQuantity(ticket) <= 0)
            messages.Add("Proposal price and quantity are required.");
        else if (!FoleoTraderFillAllocator.IsWholeQuantity(ProposalQuantity(ticket)))
            messages.Add("FoleoTrader order quantity must be a positive whole number.");
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

    private (ITicket? TradeEvent, IReadOnlyList<string> ValidationErrors) CreateProratedTradeEvent(
        FoleoTraderExecutionReport report,
        TicketNumber ticketNumber,
        EventDateTime eventDateTime,
        Ticket ticket,
        Tickets tickets,
        Holdings holdings,
        Instruments instruments,
        decimal fillSettlementAmount)
    {
        var proposalTotal = ProposalQuantity(ticket);
        if (proposalTotal <= 0m)
            return (null, ["Proposal allocations are required before FoleoTrader fills can update trade allocations."]);

        var totalQuantity = Round(ticket.Fills.Sum(fill => fill.Quantity) + report.LastQty);
        var totalSettlementAmount = Round(ticket.Fills.Sum(fill => fill.SettlementAmount.Value) + fillSettlementAmount);
        if (totalQuantity <= 0m || totalSettlementAmount <= 0m)
            return (null, ["FoleoTrader fill totals must be greater than zero."]);

        if (!FoleoTraderFillAllocator.IsWholeQuantity(totalQuantity))
            return (null, ["FoleoTrader fill total quantity must be a positive whole number."]);

        var quantities = FoleoTraderFillAllocator.ProrateWholeQuantity(totalQuantity, ticket.ProposalAllocations);
        var settlementAmounts = FoleoTraderFillAllocator.ProrateAmountByQuantities(totalSettlementAmount, quantities, DecimalScale);
        var allocations = ticket.ProposalAllocations
            .Select((allocation, index) => new TicketTradeAllocation(
                allocation.AccountID,
                quantities[index],
                settlementAmounts[index],
                ResolveCashHoldingID(allocation.AccountID, ticket, holdings, instruments)))
            .Where(allocation => allocation.Quantity > 0m)
            .ToList();

        var request = new TicketTradeRequest(
            Constants.Initialisation.UserID,
            eventDateTime,
            $"FoleoTrader FIX trade allocation {report.ExecID}",
            ticketNumber,
            new Price(Round(totalSettlementAmount / totalQuantity)),
            eventDateTime,
            NextWorkingDaySettlement(eventDateTime),
            allocations);

        if (ticket.TradeAllocations.Count == 0)
        {
            var createResult = TicketEventBuilder.CreateTrade(request, tickets, holdings, instruments);
            if (createResult.IsValid && createResult.Value is not null)
                return (createResult.Value, []);

            logger.LogWarning("Rejected FoleoTrader trade allocation {ExecID}: {ValidationErrors}.", report.ExecID, string.Join(" ", createResult.ValidationErrors));
            return (null, createResult.ValidationErrors);
        }

        var modifyResult = TicketEventBuilder.ModifyTrade(request, tickets, holdings, instruments);
        if (modifyResult.IsValid && modifyResult.Value is not null)
            return (modifyResult.Value, []);

        logger.LogWarning("Rejected FoleoTrader trade allocation {ExecID}: {ValidationErrors}.", report.ExecID, string.Join(" ", modifyResult.ValidationErrors));
        return (null, modifyResult.ValidationErrors);
    }

    private static HoldingID? ResolveCashHoldingID(AccountID accountID, Ticket ticket, Holdings holdings, Instruments instruments)
    {
        var existingCashHoldingID = ticket.TradeAllocations
            .FirstOrDefault(allocation => allocation.AccountID == accountID)
            ?.CashHoldingID;
        var cashInvestableKind = HoldingKindRuntime.GetKindName<HoldingCashInvestable>();
        var eligibleHoldings = holdings.Items
            .Where(holding =>
                holding.AccountID == accountID &&
                holding.Active &&
                holding.GetHoldingKindName() == cashInvestableKind &&
                instruments.Items.Any(instrument => instrument.InstrumentID == holding.InstrumentID && instrument.PriceCurrency == ticket.TradeCurrency))
            .ToList();

        if (existingCashHoldingID is not null && eligibleHoldings.Any(holding => holding.HoldingID == existingCashHoldingID))
            return existingCashHoldingID;

        return eligibleHoldings.FirstOrDefault(holding => holding.Default)?.HoldingID
            ?? (eligibleHoldings.Count == 1 ? eligibleHoldings[0].HoldingID : null);
    }

    private static SettlementDateTime NextWorkingDaySettlement(EventDateTime tradeDateTime)
    {
        var next = tradeDateTime.Value.Date.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);

        return SettlementDateTimeBuilder.Create(DateTime.SpecifyKind(next, tradeDateTime.Value.Kind));
    }

    private static decimal Round(decimal value) =>
        decimal.Round(value, DecimalScale, MidpointRounding.AwayFromZero);

    private static decimal ProposalQuantity(Ticket ticket) =>
        ticket.ProposalAllocations.Sum(allocation => allocation.Quantity);
}
