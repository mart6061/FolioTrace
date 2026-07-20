using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using API.Auth;
using API.FoleoTrader;
using API.TradeFiles;
using Repository;
using Services;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace API;

public static partial class ApiEndpointRegistration
{
    private static void MapTransactionEventEndpoints(this RouteGroupBuilder api)
    {
        var transactionEvents = api.MapGroup("/Events/Transaction").WithTags("Transaction Events");

        transactionEvents.MapGet("/", async (Guid? eventSetID, Guid? holdingID, Guid? accountID, Guid? instrumentID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, cancellationToken);

            if (eventSetID.HasValue)
                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.EventSetID.Value == eventSetID.Value,
                        TransactionCancellationEvent cancellationEvent => cancellationEvent.EventSetID.Value == eventSetID.Value,
                        TransactionBookCostAdjustedEvent adjustmentEvent => adjustmentEvent.EventSetID.Value == eventSetID.Value,
                        _ => false
                    })
                    .ToList();

            if (holdingID.HasValue)
            {
                var holdingMovementEventIds = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.HoldingID?.Value == holdingID.Value)
                    .Select(@event => @event.EventID.Value)
                    .ToHashSet();
                var holdingMovementEventSetIds = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.HoldingID?.Value == holdingID.Value)
                    .Select(@event => @event.EventSetID.Value)
                    .ToHashSet();

                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.HoldingID?.Value == holdingID.Value,
                        TransactionCancellationEvent cancellationEvent => holdingMovementEventIds.Contains(cancellationEvent.CancelledEventID.Value) ||
                            cancellationEvent.CancelledIDGroup.Any(cancelled => holdingMovementEventIds.Contains(cancelled.Value)),
                        TransactionBookCostAdjustedEvent adjustmentEvent => holdingMovementEventSetIds.Contains(adjustmentEvent.EventSetID.Value),
                        _ => false
                    })
                    .ToList();
            }

            if (accountID.HasValue)
            {
                var accountMovementEventIds = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.AccountID.Value == accountID.Value)
                    .Select(@event => @event.EventID.Value)
                    .ToHashSet();

                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.AccountID.Value == accountID.Value,
                        TransactionCancellationEvent cancellationEvent => cancellationEvent.AccountID?.Value == accountID.Value ||
                            cancellationEvent.CancelledIDGroup.Any(cancelled => accountMovementEventIds.Contains(cancelled.Value)),
                        TransactionBookCostAdjustedEvent adjustmentEvent => adjustmentEvent.AccountID.Value == accountID.Value,
                        _ => false
                    })
                    .ToList();
            }

            if (instrumentID.HasValue)
            {
                var instrumentMovementEventSetIds = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.InstrumentID.Value == instrumentID.Value)
                    .Select(@event => @event.EventSetID.Value)
                    .ToHashSet();

                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.InstrumentID.Value == instrumentID.Value,
                        TransactionBookCostAdjustedEvent adjustmentEvent => instrumentMovementEventSetIds.Contains(adjustmentEvent.EventSetID.Value),
                        _ => false
                    })
                    .ToList();
            }

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToTransactionEventResponse));
        });

        transactionEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is ITransactionEvent transactionEvent
                ? Results.Ok(ToTransactionEventResponse(transactionEvent))
                : Results.NotFound();
        });

        transactionEvents.MapPost("/TransactionSet", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TransactionSetRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var accounts = await TryGetAccounts(request.EventDateTime, asAt, eventRepository, cancellationToken);
            var holdings = await TryGetHoldings(request.EventDateTime, asAt, eventRepository, cancellationToken);
            var result = TransactionBuilder.Create(request, holdings, accounts);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            var events = result.Value.Cast<IEventBase>().ToList();
            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, events, cancellationToken);
            cacheInvalidationService.Invalidate(events);

            return Results.Accepted(TransactionEventsRoute, CreateAcceptedEventsResponse(TransactionEventsRoute, events));
        });

        transactionEvents.MapPost("/BookCostAdjustment", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TransactionBookCostAdjustmentRequest request, CancellationToken cancellationToken) =>
        {
            var existingEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, cancellationToken);
            var result = TransactionBookCostAdjustedEventBuilder.Create(request, existingEvents);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);

            return Results.Accepted(TransactionEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(TransactionEventsRoute, result.Value));
        });

        transactionEvents.MapPost("/Cancel", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TransactionCancellationRequest request, CancellationToken cancellationToken) =>
        {
            var existingEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, cancellationToken);

            Result<IReadOnlyList<TransactionCancellationEvent>> result;
            try
            {
                result = TransactionCancellationEventBuilder.Create(request, existingEvents);
            }
            catch (InvalidOperationException exception)
            {
                return Results.Conflict(Result<IReadOnlyList<TransactionCancellationEvent>>.Invalid([exception.Message]));
            }

            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            var events = result.Value.Cast<IEventBase>().ToList();
            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, events, cancellationToken);
            cacheInvalidationService.Invalidate(events);

            return Results.Accepted(TransactionEventsRoute, CreateAcceptedEventsResponse(TransactionEventsRoute, events));
        });
    }

    private static void MapTicketEventEndpoints(this RouteGroupBuilder api)
    {
        var ticketEvents = api.MapGroup("/Events/Ticket").WithTags("Ticket Events");

        ticketEvents.MapGet("/", async (int? ticketNumber, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ITicket>(Constants.Initialisation.TicketsStreamId, cancellationToken);
            if (ticketNumber.HasValue)
                events = events.Where(@event => @event.TicketNumber.Value == ticketNumber.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToTicketEventResponse));
        });

        ticketEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is ITicket ticketEvent
                ? Results.Ok(ToTicketEventResponse(ticketEvent))
                : Results.NotFound();
        });

        ticketEvents.MapPost($"/{nameof(TicketCreatedEvent)}", async (IEventRepository eventRepository, InstrumentService instrumentService, AggregateCacheInvalidationService cacheInvalidationService, TicketCreatedRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var events = await eventRepository.LoadStreamAsync<ITicket>(Constants.Initialisation.TicketsStreamId, cancellationToken);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.Create(request, TicketEventBuilder.NextTicketNumber(events), instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketAccountAddedEvent)}", async (TicketService ticketService, AccountService accountService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketAccountRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var accounts = await accountService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.AddAccount(request, tickets, accounts);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketAccountRemovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketAccountRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.RemoveAccount(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalCreatedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketProposalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.CreateProposal(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalModifiedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketProposalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.ModifyProposal(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalDecisionRequestedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.RequestProposalDecision(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalApprovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.ApproveProposal(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalNotApprovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.NotApproveProposal(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalReasonSetEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTextSetRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.SetProposalReason(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalAllocationSetEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTextSetRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.SetProposalAllocation(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeCreatedEvent)}", async (TicketService ticketService, HoldingService holdingService, InstrumentService instrumentService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var holdings = await holdingService.Get(request.EventDateTime, asAt);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.CreateTrade(request, tickets, holdings, instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeModifiedEvent)}", async (TicketService ticketService, HoldingService holdingService, InstrumentService instrumentService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var holdings = await holdingService.Get(request.EventDateTime, asAt);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.ModifyTrade(request, tickets, holdings, instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeFillAddedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeFillRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.AddFill(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeFillModifiedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeFillRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.ModifyFill(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeFillRemovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeFillRemovedRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.RemoveFill(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeDecisionRequestedEvent)}", async (TicketService ticketService, HoldingService holdingService, InstrumentService instrumentService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var holdings = await holdingService.Get(request.EventDateTime, asAt);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.RequestTradeDecision(request, tickets, holdings, instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeApprovedEvent)}", async (TicketService ticketService, AccountService accountService, InstrumentService instrumentService, FXRateService fxRateService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeApprovalRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var tickets = await ticketService.Get(request.EventDateTime, asAt);
            var accounts = await accountService.Get(request.EventDateTime, asAt);
            var instruments = await instrumentService.Get(request.EventDateTime, asAt);
            var ticket = tickets.Find(request.TicketNumber);
            var fxRates = ticket?.TradeDateTime is null
                ? null
                : await fxRateService.Get(ticket.TradeDateTime, asAt);
            var holdingEvents = await eventRepository.LoadStreamAsync<IHoldingEvent>(Constants.Initialisation.HoldingsStreamId, cancellationToken);
            var result = TicketEventBuilder.ApproveTradeWithTransactions(request, tickets, accounts, instruments, holdingEvents, fxRates);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            if (result.Value.HoldingEvents.Count > 0)
                await eventRepository.AppendAsync(Constants.Initialisation.HoldingsStreamId, result.Value.HoldingEvents.Cast<IEventBase>().ToList(), cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.TicketsStreamId, result.Value.ApprovalEvent, cancellationToken);
            var transactionEvents = result.Value.TransactionEvents.Cast<IEventBase>().ToList();
            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, transactionEvents, cancellationToken);

            cacheInvalidationService.Invalidate([.. result.Value.HoldingEvents, result.Value.ApprovalEvent, .. transactionEvents]);

            return Results.Accepted(TicketEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(TicketEventsRoute, result.Value.ApprovalEvent));
        });

        ticketEvents.MapPost($"/{nameof(TicketTradeNotApprovedEvent)}", async (TicketService ticketService, HoldingService holdingService, InstrumentService instrumentService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var holdings = await holdingService.Get(request.EventDateTime, asAt);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.NotApproveTrade(request, tickets, holdings, instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeInstructionNotesSetEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTextSetRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.SetTradeInstructionNotes(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeProgressNotesSetEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTextSetRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.SetTradeProgressNotes(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketCancelledEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketCancellationRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.Cancel(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));
    }

    private static void MapUserEventEndpoints(this RouteGroupBuilder api)
    {
        var userEvents = api.MapGroup("/Events/User").WithTags("User Events");

        userEvents.MapGet("/", async (Guid? userID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IUserEvent>(Constants.Initialisation.UsersStreamId, cancellationToken);

            if (userID.HasValue)
                events = events.Where(@event => @event.UserID.Value == userID.Value).ToList();

            return Results.Ok(events.ToList());
        });

        userEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IUserEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        userEvents.MapPost($"/{nameof(UserCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserEventRequest request, CancellationToken cancellationToken) =>
        {
            var userResult = UserCreatedEventBuilder.Create(
                request.UserID,
                EventDateTimeBuilder.Create(request.EventDateTime),
                request.Reason,
                request.DisplayName,
                CreateUserDisplayPreferences(request),
                CreateUserValuationPreferences(request));

            if (!userResult.IsValid || userResult.Value is null)
                return Results.BadRequest(userResult);

            var menuPreferencesResult = UserMenuPreferencesCreatedEventBuilder.CreateDefault(userResult.Value);
            if (!menuPreferencesResult.IsValid || menuPreferencesResult.Value is null)
                return Results.BadRequest(menuPreferencesResult);

            var valuationPreferencesResult = UserValuationPreferencesCreatedEventBuilder.CreateDefault(userResult.Value);
            if (!valuationPreferencesResult.IsValid || valuationPreferencesResult.Value is null)
                return Results.BadRequest(valuationPreferencesResult);

            await eventRepository.AppendAsync(Constants.Initialisation.UsersStreamId, userResult.Value, cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.UserMenuPreferencesStreamId, menuPreferencesResult.Value, cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.UserValuationPreferencesStreamId, valuationPreferencesResult.Value, cancellationToken);

            cacheInvalidationService.Invalidate(userResult.Value);
            cacheInvalidationService.Invalidate(menuPreferencesResult.Value);
            cacheInvalidationService.Invalidate(valuationPreferencesResult.Value);

            return Results.Accepted(UserEventsRoute, CreateAcceptedEventsResponse([
                (UserEventsRoute, (IEventBase)userResult.Value),
                (UserMenuPreferencesEventsRoute, menuPreferencesResult.Value),
                (UserValuationPreferencesEventsRoute, valuationPreferencesResult.Value)
            ]));
        });

        userEvents.MapPost($"/{nameof(UserModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UsersStreamId,
                UserEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserModifiedEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason,
                    request.DisplayName,
                    CreateUserDisplayPreferences(request),
                    CreateUserValuationPreferences(request)),
                cancellationToken));

        userEvents.MapPost($"/{nameof(UserSignedInEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserSessionEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UsersStreamId,
                UserEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserSignedInEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason),
                cancellationToken));

        userEvents.MapPost($"/{nameof(UserSignedOutEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserSessionEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UsersStreamId,
                UserEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserSignedOutEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason),
                cancellationToken));
    }

    private static void MapUserMenuPreferencesEventEndpoints(this RouteGroupBuilder api)
    {
        var userMenuPreferencesEvents = api.MapGroup("/Events/UserMenuPreferences").WithTags("User Menu Preferences Events");

        userMenuPreferencesEvents.MapGet("/", async (Guid? userID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IUserMenuPreferencesEvent>(Constants.Initialisation.UserMenuPreferencesStreamId, cancellationToken);

            if (userID.HasValue)
                events = events.Where(@event => @event.UserID.Value == userID.Value).ToList();

            return Results.Ok(events.ToList());
        });

        userMenuPreferencesEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IUserMenuPreferencesEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        userMenuPreferencesEvents.MapPost($"/{nameof(UserMenuPreferencesCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserMenuPreferencesRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserMenuPreferencesStreamId,
                UserMenuPreferencesEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserMenuPreferencesCreatedEventBuilder.Create(request),
                cancellationToken));

        userMenuPreferencesEvents.MapPost($"/{nameof(UserMenuPreferencesModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserMenuPreferencesRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserMenuPreferencesStreamId,
                UserMenuPreferencesEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserMenuPreferencesModifiedEventBuilder.Create(request),
                cancellationToken));
    }

    private static void MapUserValuationPreferencesEventEndpoints(this RouteGroupBuilder api)
    {
        var userValuationPreferencesEvents = api.MapGroup("/Events/UserValuationPreferences").WithTags("User Valuation Preferences Events");

        userValuationPreferencesEvents.MapGet("/", async (Guid? userID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IUserValuationPreferencesEvent>(Constants.Initialisation.UserValuationPreferencesStreamId, cancellationToken);

            if (userID.HasValue)
                events = events.Where(@event => @event.UserID.Value == userID.Value).ToList();

            return Results.Ok(events.ToList());
        });

        userValuationPreferencesEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IUserValuationPreferencesEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        userValuationPreferencesEvents.MapPost($"/{nameof(UserValuationPreferencesCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserValuationPreferencesRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserValuationPreferencesStreamId,
                UserValuationPreferencesEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserValuationPreferencesCreatedEventBuilder.Create(request),
                cancellationToken));

        userValuationPreferencesEvents.MapPost($"/{nameof(UserValuationPreferencesModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserValuationPreferencesRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserValuationPreferencesStreamId,
                UserValuationPreferencesEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserValuationPreferencesModifiedEventBuilder.Create(request),
                cancellationToken));
    }

    private static void MapUserBookmarksEventEndpoints(this RouteGroupBuilder api)
    {
        var userBookmarksEvents = api.MapGroup("/Events/UserBookmarks").WithTags("User Bookmark Events");

        userBookmarksEvents.MapGet("/", async (Guid? userID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IUserBookmarksEvent>(Constants.Initialisation.UserBookmarksStreamId, cancellationToken);

            if (userID.HasValue)
                events = events.Where(@event => @event.UserID.Value == userID.Value).ToList();

            return Results.Ok(events.ToList());
        });

        userBookmarksEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IUserBookmarksEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        userBookmarksEvents.MapPost($"/{nameof(UserBookmarkCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserBookmarkRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserBookmarksStreamId,
                UserBookmarksEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserBookmarkCreatedEventBuilder.Create(request),
                cancellationToken));

        userBookmarksEvents.MapPost($"/{nameof(UserBookmarkModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserBookmarkRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserBookmarksStreamId,
                UserBookmarksEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserBookmarkModifiedEventBuilder.Create(request),
                cancellationToken));

        userBookmarksEvents.MapPost($"/{nameof(UserBookmarkDisplayOrderSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserBookmarkDisplayOrderSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserBookmarksStreamId,
                UserBookmarksEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserBookmarkDisplayOrderSetEventBuilder.Create(request),
                cancellationToken));

        userBookmarksEvents.MapPost($"/{nameof(UserBookmarkDeletedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserBookmarkDeletedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserBookmarksStreamId,
                UserBookmarksEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserBookmarkDeletedEventBuilder.Create(request),
                cancellationToken));
    }

}
