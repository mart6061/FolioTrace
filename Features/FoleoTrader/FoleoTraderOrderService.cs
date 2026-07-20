using FolioTrace.Types;
using Repository;

namespace FolioTrace.Aggregates;

public sealed class FoleoTraderOrderService(IEventRepository eventRepository)
{
    private const int DecimalScale = 8;

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

    public FoleoTraderOrderValidationResult ValidateOrder(
        FoleoTraderOrderRequest request,
        Tickets tickets,
        Instruments instruments,
        FoleoTraderOrders existingOrders)
    {
        var messages = new List<string>();
        var ticket = tickets.Find(request.TicketNumber);
        Instrument? instrument = null;
        var securityID = string.Empty;
        var securityIDSource = string.Empty;
        var symbol = string.Empty;

        if (ticket is null)
        {
            messages.Add($"No matching ticket found for TicketNumber '{request.TicketNumber.Value}'.");
            return new(messages, null, null, securityID, securityIDSource, symbol);
        }

        instrument = instruments.Items.FirstOrDefault(item => item.InstrumentID == ticket.InstrumentID);
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

        var proposalQuantity = ProposalQuantity(ticket);
        if (ticket.ProposalTargetPrice is null || proposalQuantity <= 0)
            messages.Add("Proposal price and quantity are required.");
        else if (!FoleoTraderFillAllocator.IsWholeQuantity(proposalQuantity))
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

        return new(messages, ticket, instrument, securityID, securityIDSource, symbol);
    }

    public FoleoTraderTradeResult CreateProratedTradeEvent(
        string execID,
        decimal fillQuantity,
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
            return new(null, ["Proposal allocations are required before FoleoTrader fills can update trade allocations."]);

        var totalQuantity = Round(ticket.Fills.Sum(fill => fill.Quantity) + fillQuantity);
        var totalSettlementAmount = Round(ticket.Fills.Sum(fill => fill.SettlementAmount.Value) + fillSettlementAmount);
        if (totalQuantity <= 0m || totalSettlementAmount <= 0m)
            return new(null, ["FoleoTrader fill totals must be greater than zero."]);
        if (!FoleoTraderFillAllocator.IsWholeQuantity(totalQuantity))
            return new(null, ["FoleoTrader fill total quantity must be a positive whole number."]);

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
            $"FoleoTrader FIX trade allocation {execID}",
            ticketNumber,
            new Price(Round(totalSettlementAmount / totalQuantity)),
            eventDateTime,
            NextWorkingDaySettlement(eventDateTime),
            allocations);

        if (ticket.TradeAllocations.Count == 0)
        {
            var createResult = TicketEventBuilder.CreateTrade(request, tickets, holdings, instruments, allowExecutionLocked: true);
            return createResult.IsValid && createResult.Value is not null
                ? new(createResult.Value, [])
                : new(null, createResult.ValidationErrors);
        }

        var modifyResult = TicketEventBuilder.ModifyTrade(request, tickets, holdings, instruments, allowExecutionLocked: true);
        return modifyResult.IsValid && modifyResult.Value is not null
            ? new(modifyResult.Value, [])
            : new(null, modifyResult.ValidationErrors);
    }

    public static SettlementDateTime NextWorkingDaySettlement(EventDateTime tradeDateTime)
    {
        var next = tradeDateTime.Value.Date.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);

        return SettlementDateTimeBuilder.Create(DateTime.SpecifyKind(next, tradeDateTime.Value.Kind));
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

    private static decimal Round(decimal value) =>
        decimal.Round(value, DecimalScale, MidpointRounding.AwayFromZero);

    public static decimal ProposalQuantity(Ticket ticket) =>
        ticket.ProposalAllocations.Sum(allocation => allocation.Quantity);
}
