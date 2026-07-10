using System.Security.Cryptography;
using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Microsoft.Extensions.Options;
using API.FoleoTrader;
using Repository;
using Services;

namespace API.TradeFiles;

public sealed class TradeFileWorkflowService(
    IEventRepository repository,
    TicketService ticketService,
    BrokerService brokerService,
    InstrumentService instrumentService,
    HoldingService holdingService,
    TradeFileService tradeFileService,
    AggregateCacheInvalidationService cacheInvalidationService,
    TradeFileWorkbookGenerator workbookGenerator,
    IEnumerable<ITradeFileSender> senders,
    IOptions<TradeFileOptions> options)
{
    private const string SpreadsheetMediaType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly SemaphoreSlim processingLock = new(1, 1);

    public async Task<TradeFileID> RequestAsync(TradeFileRequest request, CancellationToken cancellationToken)
    {
        if (request.TicketNumbers.Count == 0)
            throw new ArgumentException("At least one ticket is required.");
        if (request.TicketNumbers.Distinct().Count() != request.TicketNumbers.Count)
            throw new ArgumentException("A ticket may only be included once.");

        var asOf = AuditDateTimeBuilder.Create();
        var tickets = await ticketService.Get(request.EventDateTime, asOf);
        var brokers = await brokerService.Get(request.EventDateTime, asOf);
        var instruments = await instrumentService.Get(request.EventDateTime, asOf);
        var broker = brokers.Items.SingleOrDefault(item => item.LEI == request.BrokerLEI && item.Active)
            ?? throw new ArgumentException("The selected broker was not found or is inactive.");
        var method = broker.TradeMethods.OfType<TradeFileTradeMethod>().SingleOrDefault()
            ?? throw new ArgumentException("The selected broker does not have an enabled TradeFile method.");

        var snapshots = new List<TradeFileTicketSnapshot>();
        foreach (var ticketNumber in request.TicketNumbers)
        {
            var ticket = tickets.Find(ticketNumber) ?? throw new ArgumentException($"Ticket {ticketNumber.Value} was not found.");
            if (ticket.TradeExecutionStatus != TicketTradeExecutionStatus.PendingTradeFile ||
                ticket.TradeExecutionMethod != TradeMethodType.TradeFile ||
                ticket.ExecutionBrokerLEI != broker.LEI)
                throw new ArgumentException($"Ticket {ticketNumber.Value} is not pending for the selected TradeFile broker.");
            if (ticket.ProposalTargetPrice is null || ticket.ProposalAllocations.Count == 0)
                throw new ArgumentException($"Ticket {ticketNumber.Value} has no approved proposal details.");

            var instrument = instruments.Items.SingleOrDefault(item => item.InstrumentID == ticket.InstrumentID)
                ?? throw new ArgumentException($"The instrument for ticket {ticketNumber.Value} was not found.");
            var isin = instrument.Identifiers.SingleOrDefault(item => item.Type == InstrumentIdentifierType.ISIN)?.Value ?? string.Empty;
            var sedol = instrument.Identifiers.SingleOrDefault(item => item.Type == InstrumentIdentifierType.Sedol)?.Value ?? string.Empty;
            if (method.Columns.Contains(TradeFileColumn.ISIN) && string.IsNullOrWhiteSpace(isin))
                throw new ArgumentException($"Ticket {ticketNumber.Value} requires an ISIN.");
            if (method.Columns.Contains(TradeFileColumn.Sedol) && string.IsNullOrWhiteSpace(sedol))
                throw new ArgumentException($"Ticket {ticketNumber.Value} requires a Sedol.");

            snapshots.Add(new(ticket.TicketNumber, isin, sedol, ticket.ProposalAllocations.Sum(item => item.Quantity), ticket.ProposalTargetPrice, ticket.TradeCurrency));
        }

        var tradeFileID = new TradeFileID(Guid.CreateGuid7());
        var audit = AuditDateTimeBuilder.Create();
        var requested = new TradeFileRequestedEvent(new(Guid.CreateGuid7()), request.UserID, request.EventDateTime, audit, request.Reason, tradeFileID, broker.LEI, broker.Name, method.FileNameTemplate, [.. method.Columns], method.SendConfig, snapshots);
        var ticketEvents = snapshots.Select(snapshot => (IAuditEventBase)new TicketTradeFileRequestedEvent(new(Guid.CreateGuid7()), request.UserID, request.EventDateTime, audit, request.Reason, snapshot.TicketNumber, broker.LEI, tradeFileID)).ToList();
        await repository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>>
        {
            [Constants.Initialisation.TradeFilesStreamId] = [requested],
            [Constants.Initialisation.TicketsStreamId] = ticketEvents
        }, cancellationToken: cancellationToken);
        cacheInvalidationService.Invalidate([requested, .. ticketEvents]);
        return tradeFileID;
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        if (!await processingLock.WaitAsync(0, cancellationToken)) return;
        try
        {
            var tradeFiles = await tradeFileService.Get(ReferenceDataCurrent.EndOfToday(), AuditDateTimeBuilder.Create(), cancellationToken);
            foreach (var tradeFile in tradeFiles.Items.Where(item => item.Status is TradeFileStatus.Requested or TradeFileStatus.Created).ToList())
            {
                try
                {
                    var current = tradeFile;
                    if (current.Status == TradeFileStatus.Requested)
                        current = await CreateAsync(current, cancellationToken);
                    if (current.Status == TradeFileStatus.Created)
                        await SendAsync(current, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    await FailAsync(tradeFile, exception.Message, cancellationToken);
                }
            }
        }
        finally { processingLock.Release(); }
    }

    private async Task<TradeFile> CreateAsync(TradeFile tradeFile, CancellationToken cancellationToken)
    {
        var requested = await RequestedEventAsync(tradeFile.TradeFileID, cancellationToken);
        var (fileName, content) = workbookGenerator.Generate(requested, DateTime.UtcNow);
        var storedFileID = new StoredFileID(Guid.CreateGuid7());
        var sha = Convert.ToHexString(SHA256.HashData(content));
        var audit = AuditDateTimeBuilder.Create();
        var storedEvent = new StoredFileCreatedEvent(new(Guid.CreateGuid7()), requested.UserID, requested.EventDateTime, audit, "Create TradeFile payload", storedFileID, fileName, SpreadsheetMediaType, content.LongLength, sha);
        var created = new TradeFileCreatedEvent(new(Guid.CreateGuid7()), requested.UserID, requested.EventDateTime, audit, "Create TradeFile", tradeFile.TradeFileID, storedFileID, fileName, SpreadsheetMediaType, content.LongLength, sha);
        var ticketEvents = tradeFile.Tickets.Select(ticket => (IAuditEventBase)new TicketTradeFileCreatedEvent(new(Guid.CreateGuid7()), requested.UserID, requested.EventDateTime, audit, "Create TradeFile", ticket.TicketNumber, tradeFile.BrokerLEI, tradeFile.TradeFileID)).ToList();
        await repository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>>
        {
            [Constants.Initialisation.StoredFilesStreamId] = [storedEvent],
            [Constants.Initialisation.TradeFilesStreamId] = [created],
            [Constants.Initialisation.TicketsStreamId] = ticketEvents
        }, new StoredFilePayload(storedFileID.Value, fileName, SpreadsheetMediaType, sha, content), cancellationToken);
        cacheInvalidationService.Invalidate([storedEvent, created, .. ticketEvents]);
        return tradeFile with { Status = TradeFileStatus.Created, StoredFileID = storedFileID, FileName = fileName };
    }

    private async Task SendAsync(TradeFile tradeFile, CancellationToken cancellationToken)
    {
        if (tradeFile.StoredFileID is null) throw new InvalidOperationException("The TradeFile has no stored payload.");
        var payload = await repository.LoadStoredFileAsync(tradeFile.StoredFileID.Value, cancellationToken)
            ?? throw new InvalidOperationException("The TradeFile payload was not found.");
        var sender = senders.SingleOrDefault(item => item.Supports(tradeFile.SendConfig))
            ?? throw new InvalidOperationException($"No sender supports {tradeFile.SendConfig.GetType().Name}.");
        var callbackBase = options.Value.ApiCallbackBaseUrl.TrimEnd('/');
        var requested = await RequestedEventAsync(tradeFile.TradeFileID, cancellationToken);
        var audit = AuditDateTimeBuilder.Create();
        var sent = new TradeFileSentEvent(new(Guid.CreateGuid7()), requested.UserID, requested.EventDateTime, audit, "Send TradeFile", tradeFile.TradeFileID);
        var ticketEvents = tradeFile.Tickets.Select(ticket => (IAuditEventBase)new TicketTradeFileSentEvent(new(Guid.CreateGuid7()), requested.UserID, requested.EventDateTime, audit, "Send TradeFile", ticket.TicketNumber, tradeFile.BrokerLEI, tradeFile.TradeFileID)).ToList();
        await repository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>>
        {
            [Constants.Initialisation.TradeFilesStreamId] = [sent],
            [Constants.Initialisation.TicketsStreamId] = ticketEvents
        }, cancellationToken: cancellationToken);
        cacheInvalidationService.Invalidate([sent, .. ticketEvents]);
        await sender.SendAsync(new TradeFileDeliveryRequest(
            tradeFile.TradeFileID.Value,
            tradeFile.BrokerLEI.Value,
            payload.FileName,
            payload.MediaType,
            payload.Content,
            $"{callbackBase}/Acknowledgements",
            $"{callbackBase}/Confirmations",
            tradeFile.Tickets.Select(item => new TradeFileDeliveryTicket(item.TicketNumber.Value, item.Quantity, item.Price.Amount)).ToList()), cancellationToken);
    }

    public async Task AcknowledgeAsync(TradeFileReceivedConfirm confirmation, CancellationToken cancellationToken)
    {
        var stream = await repository.LoadStreamAsync<ITradeFileEvent>(Constants.Initialisation.TradeFilesStreamId, cancellationToken);
        if (stream.OfType<TradeFileAcknowledgedEvent>().Any(item => item.ConfirmationID == confirmation.ConfirmationID)) return;
        var requested = stream.OfType<TradeFileRequestedEvent>().SingleOrDefault(item => item.TradeFileID.Value == confirmation.TradeFileID)
            ?? throw new ArgumentException("TradeFile was not found.");
        if (!string.Equals(requested.BrokerLEI.Value, confirmation.BrokerLEI, StringComparison.Ordinal))
            throw new ArgumentException("Broker LEI does not match the TradeFile.");
        var aggregate = new FolioTrace.Aggregates.TradeFiles(ReferenceDataCurrent.EndOfToday(), AuditDateTimeBuilder.Create(), stream);
        var tradeFile = aggregate.Find(new TradeFileID(confirmation.TradeFileID)) ?? throw new ArgumentException("TradeFile was not found.");
        if (tradeFile.Status is TradeFileStatus.Acknowledged or TradeFileStatus.InProgress or TradeFileStatus.Completed) return;
        if (tradeFile.Status != TradeFileStatus.Sent) throw new InvalidOperationException("Only sent TradeFiles can be acknowledged.");

        var eventDate = new EventDateTime(confirmation.ReceivedAtUtc.ToUniversalTime());
        var audit = AuditDateTimeBuilder.Create();
        var acknowledged = new TradeFileAcknowledgedEvent(new(Guid.CreateGuid7()), requested.UserID, eventDate, audit, "FoleoTrader acknowledged TradeFile", tradeFile.TradeFileID, confirmation.ConfirmationID);
        var ticketEvents = tradeFile.Tickets.Select(ticket => (IAuditEventBase)new TicketTradeFileAcknowledgedEvent(new(Guid.CreateGuid7()), requested.UserID, eventDate, audit, "FoleoTrader acknowledged TradeFile", ticket.TicketNumber, tradeFile.BrokerLEI, tradeFile.TradeFileID)).ToList();
        await repository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>>
        {
            [Constants.Initialisation.TradeFilesStreamId] = [acknowledged],
            [Constants.Initialisation.TicketsStreamId] = ticketEvents
        }, cancellationToken: cancellationToken);
        cacheInvalidationService.Invalidate([acknowledged, .. ticketEvents]);
    }

    public async Task ConfirmAsync(TradeFileTradeConfirm confirmation, CancellationToken cancellationToken)
    {
        var stream = await repository.LoadStreamAsync<ITradeFileEvent>(Constants.Initialisation.TradeFilesStreamId, cancellationToken);
        if (stream.OfType<TradeFileTicketConfirmedEvent>().Any(item => item.ConfirmationID == confirmation.ConfirmationID)) return;
        var requested = stream.OfType<TradeFileRequestedEvent>().SingleOrDefault(item => item.TradeFileID.Value == confirmation.TradeFileID)
            ?? throw new ArgumentException("TradeFile was not found.");
        var aggregate = new FolioTrace.Aggregates.TradeFiles(ReferenceDataCurrent.EndOfToday(), AuditDateTimeBuilder.Create(), stream);
        var tradeFile = aggregate.Find(new TradeFileID(confirmation.TradeFileID)) ?? throw new ArgumentException("TradeFile was not found.");
        var ticketNumber = new TicketNumber(confirmation.TicketNumber);
        var snapshot = tradeFile.Tickets.SingleOrDefault(item => item.TicketNumber == ticketNumber)
            ?? throw new ArgumentException("Ticket does not belong to this TradeFile.");
        if (tradeFile.ConfirmedTickets.Contains(ticketNumber)) return;
        if (confirmation.Quantity <= 0 || confirmation.Quantity > snapshot.Quantity)
            throw new ArgumentException("Confirmed quantity is outside the requested quantity.");

        var eventDate = new EventDateTime(confirmation.ConfirmedAtUtc.ToUniversalTime());
        var audit = AuditDateTimeBuilder.Create();
        var asOf = AuditDateTimeBuilder.Create();
        var tickets = await ticketService.Get(eventDate, asOf);
        var ticket = tickets.Find(ticketNumber) ?? throw new ArgumentException("Ticket was not found.");
        var holdings = await holdingService.Get(eventDate, asOf);
        var instruments = await instrumentService.Get(eventDate, asOf);
        var tradeEvent = CreateTradeEvent(ticket, tickets, holdings, instruments, confirmation.Quantity, confirmation.Price, eventDate, requested.UserID, confirmation.ConfirmationID);
        var settlementAmount = decimal.Round(confirmation.Quantity * confirmation.Price, 8, MidpointRounding.AwayFromZero);
        var fillResult = TicketEventBuilder.AddFill(new TicketTradeFillRequest(
            requested.UserID,
            eventDate,
            $"TradeFile confirmation {confirmation.ConfirmationID}",
            ticketNumber,
            Guid.CreateGuid7(),
            tradeFile.BrokerLEI,
            new Price(confirmation.Price),
            confirmation.Quantity,
            new TransactionLocalCost(settlementAmount),
            $"TradeFile {tradeFile.TradeFileID}"), tickets, allowExecutionLocked: true);
        if (!fillResult.IsValid || fillResult.Value is null)
            throw new InvalidOperationException(string.Join(" ", fillResult.ValidationErrors));
        var confirmed = new TradeFileTicketConfirmedEvent(new(Guid.CreateGuid7()), requested.UserID, eventDate, audit, "FoleoTrader confirmed TradeFile ticket", tradeFile.TradeFileID, confirmation.ConfirmationID, ticketNumber, confirmation.Quantity, new Price(confirmation.Price));
        var inProgress = new TicketTradeExecutionInProgressEvent(new(Guid.CreateGuid7()), requested.UserID, eventDate, audit, "TradeFile execution confirmed", ticketNumber, TradeMethodType.TradeFile, tradeFile.BrokerLEI, tradeFile.TradeFileID);
        var tradeFileEvents = new List<IAuditEventBase> { confirmed };
        if (tradeFile.ConfirmedTickets.Count + 1 == tradeFile.Tickets.Count)
            tradeFileEvents.Add(new TradeFileCompletedEvent(new(Guid.CreateGuid7()), requested.UserID, eventDate, audit, "All TradeFile tickets confirmed", tradeFile.TradeFileID));
        await repository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>>
        {
            [Constants.Initialisation.TradeFilesStreamId] = tradeFileEvents,
            [Constants.Initialisation.TicketsStreamId] = [tradeEvent, fillResult.Value, inProgress]
        }, cancellationToken: cancellationToken);
        cacheInvalidationService.Invalidate([.. tradeFileEvents, tradeEvent, fillResult.Value, inProgress]);
    }

    private static ITicket CreateTradeEvent(Ticket ticket, Tickets tickets, Holdings holdings, Instruments instruments, decimal quantity, decimal price, EventDateTime eventDate, UserID userID, Guid confirmationID)
    {
        var proposalTotal = ticket.ProposalAllocations.Sum(item => item.Quantity);
        if (proposalTotal <= 0m) throw new InvalidOperationException("Proposal allocations are required.");
        var quantities = ticket.ProposalAllocations.Select(item => decimal.Round(quantity * item.Quantity / proposalTotal, 8, MidpointRounding.AwayFromZero)).ToList();
        var settlementTotal = decimal.Round(quantity * price, 8, MidpointRounding.AwayFromZero);
        var settlements = FoleoTraderFillAllocator.ProrateAmountByQuantities(settlementTotal, quantities, 8);
        var allocations = ticket.ProposalAllocations.Select((allocation, index) => new TicketTradeAllocation(
            allocation.AccountID,
            quantities[index],
            settlements[index],
            ResolveCashHoldingID(allocation.AccountID, ticket, holdings, instruments))).Where(item => item.Quantity > 0).ToList();
        var settlement = eventDate.Value.Date.AddDays(1);
        while (settlement.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) settlement = settlement.AddDays(1);
        var request = new TicketTradeRequest(userID, eventDate, $"TradeFile allocation {confirmationID}", ticket.TicketNumber, new Price(price), eventDate, SettlementDateTimeBuilder.Create(DateTime.SpecifyKind(settlement, eventDate.Value.Kind)), allocations);
        var result = TicketEventBuilder.CreateTrade(request, tickets, holdings, instruments, allowExecutionLocked: true);
        if (!result.IsValid || result.Value is null) throw new InvalidOperationException(string.Join(" ", result.ValidationErrors));
        return result.Value;
    }

    private static HoldingID? ResolveCashHoldingID(AccountID accountID, Ticket ticket, Holdings holdings, Instruments instruments)
    {
        var kind = HoldingKindRuntime.GetKindName<HoldingCashInvestable>();
        var eligible = holdings.Items.Where(holding => holding.AccountID == accountID && holding.Active && holding.GetHoldingKindName() == kind &&
            instruments.Items.Any(instrument => instrument.InstrumentID == holding.InstrumentID && instrument.PriceCurrency == ticket.TradeCurrency)).ToList();
        return eligible.FirstOrDefault(item => item.Default)?.HoldingID ?? (eligible.Count == 1 ? eligible[0].HoldingID : null);
    }

    private async Task FailAsync(TradeFile tradeFile, string error, CancellationToken cancellationToken)
    {
        var requested = await RequestedEventAsync(tradeFile.TradeFileID, cancellationToken);
        var audit = AuditDateTimeBuilder.Create();
        var failed = new TradeFileFailedEvent(new(Guid.CreateGuid7()), requested.UserID, requested.EventDateTime, audit, "TradeFile workflow failed", tradeFile.TradeFileID, error);
        var ticketEvents = tradeFile.Tickets.Select(ticket => (IAuditEventBase)new TicketTradeExecutionFailedEvent(new(Guid.CreateGuid7()), requested.UserID, requested.EventDateTime, audit, "TradeFile workflow failed", ticket.TicketNumber, TradeMethodType.TradeFile, tradeFile.BrokerLEI, tradeFile.TradeFileID, error)).ToList();
        await repository.AppendWorkflowAsync(new Dictionary<Guid, IReadOnlyList<IAuditEventBase>>
        {
            [Constants.Initialisation.TradeFilesStreamId] = [failed],
            [Constants.Initialisation.TicketsStreamId] = ticketEvents
        }, cancellationToken: cancellationToken);
        cacheInvalidationService.Invalidate([failed, .. ticketEvents]);
    }

    private async Task<TradeFileRequestedEvent> RequestedEventAsync(TradeFileID id, CancellationToken cancellationToken) =>
        (await repository.LoadStreamAsync<ITradeFileEvent>(Constants.Initialisation.TradeFilesStreamId, cancellationToken))
            .OfType<TradeFileRequestedEvent>()
            .Single(item => item.TradeFileID == id);
}
