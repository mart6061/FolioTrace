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
    private static bool TryParseInputControlKinds(string? controlKind, string? controlKinds, out IReadOnlyList<InputControlKind> resolvedControlKinds, out string parseError)
    {
        var values = new[] { controlKind, controlKinds }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        if (values.Count == 0)
        {
            resolvedControlKinds = [InputControlKind.Quantity, InputControlKind.Money];
            parseError = string.Empty;
            return true;
        }

        var parsedValues = new List<InputControlKind>();
        foreach (var value in values)
        {
            if (!Enum.TryParse<InputControlKind>(value, ignoreCase: true, out var parsedValue) || !Enum.IsDefined(parsedValue))
            {
                resolvedControlKinds = [];
                parseError = $"Unknown input control kind '{value}'.";
                return false;
            }

            parsedValues.Add(parsedValue);
        }

        resolvedControlKinds = parsedValues.Distinct().ToList();
        parseError = string.Empty;
        return true;
    }

    private static UserDisplayPreferences CreateUserDisplayPreferences(UserEventRequest request) =>
        new(request.DisplayPreferences.DarkMode, request.DisplayPreferences.RememberTraceDate);

    private static UserProfileValuationPreferences CreateUserValuationPreferences(UserEventRequest request) =>
        new(
            EventDateTimeBuilder.Create(request.ValuationPreferences.ValuationDate),
            request.ValuationPreferences.ShowIncome,
            request.ValuationPreferences.ShowBook);

    private static Guid? GetAccountID(IAccountEvent @event) =>
        @event switch
        {
            AccountCreatedEvent createdEvent => createdEvent.AccountID.Value,
            AccountModifiedEvent modifiedEvent => modifiedEvent.AccountID.Value,
            AccountActiveSetEvent activeEvent => activeEvent.AccountID.Value,
            AccountDisplayOrderSetEvent displayOrderSetEvent => displayOrderSetEvent.AccountID.Value,
            AccountIdentifierSetEvent identifierSetEvent => identifierSetEvent.AccountID.Value,
            AccountIdentifierUnsetEvent identifierUnsetEvent => identifierUnsetEvent.AccountID.Value,
            _ => null
        };

    private static string? GetBrokerLEI(IBrokerEvent @event) =>
        @event switch
        {
            BrokerCreatedEvent createdEvent => createdEvent.LEI.Value,
            BrokerModifiedEvent modifiedEvent => modifiedEvent.LEI.Value,
            BrokerActiveSetEvent activeEvent => activeEvent.LEI.Value,
            BrokerApprovedDateTimeSetEvent approvedEvent => approvedEvent.LEI.Value,
            BrokerNextReviewSetEvent nextReviewEvent => nextReviewEvent.LEI.Value,
            BrokerNotesSetEvent notesEvent => notesEvent.LEI.Value,
            BrokerTradeMethodSetEventBase tradeMethodEvent => tradeMethodEvent.LEI.Value,
            BrokerTradeMethodUnsetEvent unsetEvent => unsetEvent.LEI.Value,
            _ => null
        };

    private static string? GetCountryAlpha2(ICountryEvent @event) =>
        @event switch
        {
            CountryCreatedEvent createdEvent => createdEvent.Alpha2.Value,
            CountryModifiedEvent modifiedEvent => modifiedEvent.Alpha2.Value,
            CountryFlagModifiedEvent flagEvent => flagEvent.Alpha2.Value,
            _ => null
        };

    private static string? GetCurrencyAlphabeticCode(ICurrencyEvent @event) =>
        @event switch
        {
            CurrencyCreatedEvent createdEvent => createdEvent.AlphabeticCode.Value,
            CurrencyModifiedEvent modifiedEvent => modifiedEvent.AlphabeticCode.Value,
            _ => null
        };

    private static Guid? GetAssetAllocationID(IValuationSettingEvent @event) =>
        @event.AssetAllocationID?.Value;

    private static string? GetFXPair(IFXEvent @event) =>
        @event switch
        {
            FXCreatedEvent createdEvent => createdEvent.Pair.Value,
            FXActiveModifiedEvent activeEvent => activeEvent.Pair.Value,
            _ => null
        };

    private static string? GetFXRatePair(IFXRateEvent @event) =>
        @event is FXRateSetEvent setEvent
            ? setEvent.Pair.Value
            : null;

    private static Guid? GetInstrumentID(IInstrumentEvent @event) =>
        @event switch
        {
            InstrumentCreatedEvent createdEvent => createdEvent.InstrumentID.Value,
            InstrumentModifiedEvent modifiedEvent => modifiedEvent.InstrumentID.Value,
            InstrumentActiveModifiedEvent activeEvent => activeEvent.InstrumentID.Value,
            InstrumentIdentifierSetEvent setEvent => setEvent.InstrumentID.Value,
            InstrumentIdentifierUnsetEvent unsetEvent => unsetEvent.InstrumentID.Value,
            InstrumentTermsSetEvent termsEvent => termsEvent.InstrumentID.Value,
            _ => null
        };

    private static object CreateAcceptedEventsResponse(string eventRoute, IReadOnlyList<IEventBase> events) =>
        new
        {
            EventIDs = events.Select(@event => @event.EventID.Value).ToList(),
            Links = events
                .Select(@event => new
                {
                    Rel = "self",
                    Href = $"{eventRoute}/{@event.EventID.Value}",
                    Method = "GET"
                })
                .ToList()
        };

    private static object CreateAcceptedEventsResponse(IReadOnlyList<(string EventRoute, IEventBase Event)> events) =>
        new
        {
            EventIDs = events.Select(item => item.Event.EventID.Value).ToList(),
            Links = events
                .Select(item => new
                {
                    Rel = "self",
                    Href = $"{item.EventRoute}/{item.Event.EventID.Value}",
                    Method = "GET"
                })
                .ToList()
        };

    private static async Task<IResult> AppendTicketEvent<TEvent>(
        IEventRepository eventRepository,
        AggregateCacheInvalidationService cacheInvalidationService,
        Func<Task<Result<TEvent>>> createEvent,
        CancellationToken cancellationToken)
        where TEvent : class, ITicket
    {
        var result = await createEvent();
        if (!result.IsValid || result.Value is null)
            return Results.BadRequest(result);

        await eventRepository.AppendAsync(Constants.Initialisation.TicketsStreamId, result.Value, cancellationToken);
        cacheInvalidationService.Invalidate(result.Value);

        return Results.Accepted(TicketEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(TicketEventsRoute, result.Value));
    }

    private static async Task<IReadOnlyList<string>> ValidateInstrumentValueEvent(InstrumentID instrumentID, EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var instrumentEvents = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId, cancellationToken);
        var instruments = new Instruments(eventDateTime, auditDateTime, instrumentEvents.ToList());
        var instrument = instruments.Items.SingleOrDefault(item => item.InstrumentID == instrumentID);
        if (instrument is null)
            return [$"No matching Instrument found for InstrumentID '{instrumentID}'."];

        return instrument.CFI.IsEquity || instrument.CFI.IsDebt
            ? []
            : [$"Instrument '{instrumentID}' has CFI '{instrument.CFI.Value}'. Only equity and fixed income instruments support editable v1 price and income events."];
    }

    private static async Task<Accounts?> TryGetAccounts(EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var accountEvents = await eventRepository.LoadStreamAsync<IAccountEvent>(Constants.Initialisation.AccountsStreamId, cancellationToken);
        return accountEvents.Count == 0 ? null : new Accounts(eventDateTime, auditDateTime, accountEvents.ToList());
    }

    private static async Task<Holdings?> TryGetHoldings(EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var holdingEvents = await eventRepository.LoadStreamAsync<IHoldingEvent>(Constants.Initialisation.HoldingsStreamId, cancellationToken);
        return holdingEvents.Count == 0 ? null : new Holdings(eventDateTime, auditDateTime, holdingEvents.ToList());
    }

    private static async Task<ValuationSettings?> TryGetValuationSettings(EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var valuationSettingEvents = await eventRepository.LoadStreamAsync<IValuationSettingEvent>(Constants.Initialisation.ValuationSettingsStreamId, cancellationToken);
        return valuationSettingEvents.Count == 0 ? null : new ValuationSettings(eventDateTime, auditDateTime, valuationSettingEvents.ToList());
    }

    private static async Task<ReportConfigs?> TryGetReportConfigs(EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var reportEvents = await eventRepository.LoadStreamAsync<IReportEvent>(Constants.Initialisation.ReportConfigsStreamId, cancellationToken);
        return reportEvents.Count == 0 ? null : new ReportConfigs(eventDateTime, auditDateTime, reportEvents.ToList());
    }

    private static RequestTraceResponse ToResponse(RequestTrace trace) =>
        new(
            trace.RequestId,
            trace.Source,
            trace.StartedAtUtc,
            trace.CompletedAtUtc,
            trace.DurationMilliseconds,
            trace.Method,
            trace.Path,
            trace.QueryString,
            trace.StatusCode,
            trace.HasResponse,
            trace.HasException,
            trace.LogCount,
            trace.Request is null ? null : ToResponse(trace.Request),
            trace.Response is null ? null : ToResponse(trace.Response),
            trace.Exception is null
                ? null
                : new RequestTraceExceptionResponse(
                    trace.Exception.RecordedAtUtc,
                    trace.Exception.ExceptionType,
                    trace.Exception.ExceptionMessage,
                    trace.Exception.StackTrace),
            trace.Logs.Select(log => new TraceLogEntryResponse(
                log.RecordedAtUtc,
                log.Level,
                log.Category,
                log.EventId,
                log.Message,
                log.ExceptionType,
                log.ExceptionMessage,
                log.StackTrace)).ToList());

    private static TraceHttpMessageResponse ToResponse(TraceHttpMessage message) =>
        new(message.Headers, message.Body, message.ContentType, message.ContentLength, message.BodyTruncated);

    private static RequestTraceSettingsResponse ToResponse(RequestTraceSettings settings) =>
        new(
            settings.Enabled,
            settings.CaptureApi,
            settings.CaptureUi,
            settings.CaptureBodies,
            settings.Capture500StackTraces,
            settings.CaptureLogMessages,
            settings.MinimumLogLevel,
            settings.MaximumBodyCharacters,
            settings.CapturedContentTypePrefixes,
            settings.ExcludedPathPrefixes,
            settings.RedactedHeaders);

    private static RequestTraceSettings ToSettings(RequestTraceSettingsRequest request) =>
        new()
        {
            Enabled = request.Enabled,
            CaptureApi = request.CaptureApi,
            CaptureUi = request.CaptureUi,
            CaptureBodies = request.CaptureBodies,
            Capture500StackTraces = request.Capture500StackTraces,
            CaptureLogMessages = request.CaptureLogMessages,
            MinimumLogLevel = request.MinimumLogLevel,
            MaximumBodyCharacters = request.MaximumBodyCharacters,
            CapturedContentTypePrefixes = request.CapturedContentTypePrefixes.ToArray(),
            ExcludedPathPrefixes = request.ExcludedPathPrefixes.ToArray(),
            RedactedHeaders = request.RedactedHeaders.ToArray()
        };

    private static RequestTraceEvent ToTraceEvent(RequestTraceEventIngestRequest request) =>
        new()
        {
            RequestId = request.RequestId,
            Source = string.Equals(request.Source, RequestTraceSources.Ui, StringComparison.OrdinalIgnoreCase)
                ? RequestTraceSources.Ui
                : RequestTraceSources.Api,
            Kind = request.Kind,
            RecordedAtUtc = request.RecordedAtUtc,
            StartedAtUtc = request.StartedAtUtc,
            CompletedAtUtc = request.CompletedAtUtc,
            DurationMilliseconds = request.DurationMilliseconds,
            Method = request.Method.ToUpperInvariant(),
            Path = request.Path,
            QueryString = request.QueryString,
            StatusCode = request.StatusCode,
            Message = request.Message is null
                ? null
                : new TraceHttpMessage
                {
                    Headers = request.Message.Headers,
                    Body = request.Message.Body,
                    ContentType = request.Message.ContentType,
                    ContentLength = request.Message.ContentLength,
                    BodyTruncated = request.Message.BodyTruncated
                },
            ExceptionType = request.ExceptionType,
            ExceptionMessage = request.ExceptionMessage,
            StackTrace = request.StackTrace
        };

    private static FIXOperationResponse ToResponse(FoleoTraderFIXOperationRecordedEvent @event) =>
        new(
            @event.EventID.Value,
            @event.AuditDateTime.Value,
            @event.EventDateTime.Value,
            @event.AuditDateTime.Value,
            @event.Reason,
            @event.Direction,
            @event.Channel,
            @event.SessionID,
            @event.MsgType,
            FIXMessageName(@event.MsgType),
            @event.MsgSeqNum,
            @event.SenderCompID,
            @event.TargetCompID,
            @event.SendingTime,
            @event.ClOrdID,
            @event.ExecID,
            @event.RawMessage,
            @event.RawMessage.Replace('\u0001', '|'));

    private static string FIXMessageName(string msgType) =>
        msgType switch
        {
            "0" => "Heartbeat",
            "1" => "Test Request",
            "2" => "Resend Request",
            "3" => "Reject",
            "4" => "Sequence Reset",
            "5" => "Logout",
            "8" => "Execution Report",
            "9" => "Order Cancel Reject",
            "A" => "Logon",
            "D" => "New Order Single",
            "F" => "Order Cancel Request",
            "G" => "Order Cancel/Replace Request",
            "j" => "Business Message Reject",
            _ => string.IsNullOrWhiteSpace(msgType) ? "Unknown" : msgType
        };

    private static bool IsRequestTraceStoreUnavailable(Exception exception) =>
        exception is TimeoutException ||
        exception.GetType().FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException is not null && IsRequestTraceStoreUnavailable(exception.InnerException);

    private static IResult RequestTraceUnavailable() =>
        Results.Problem(
            title: "Request trace store is unavailable.",
            detail: "The request trace database cannot be reached. Start the configured Postgres instance or update ConnectionStrings:FolioTrace.",
            statusCode: StatusCodes.Status503ServiceUnavailable);

    private static object ToInputControlSettingsEventResponse(IInputControlSettingsEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            InputControlSettingsCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                createdEvent.Settings
            },
            InputControlSettingsModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                modifiedEvent.Settings
            },
            _ => @event
        });

    private static object ToAccountEventResponse(IAccountEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            AccountCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                AccountID = createdEvent.AccountID.Value,
                createdEvent.Name,
                createdEvent.FormalName,
                BookCurrency = createdEvent.BookCurrency.Value,
                BookCostBasis = createdEvent.BookCostBasis.ToString(),
                createdEvent.Active
            },
            AccountModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                AccountID = modifiedEvent.AccountID.Value,
                modifiedEvent.Name,
                modifiedEvent.FormalName,
                BookCostBasis = modifiedEvent.BookCostBasis.ToString()
            },
            AccountActiveSetEvent activeEvent => new
            {
                Type = activeEvent.Type,
                EventID = activeEvent.EventID.Value,
                UserID = activeEvent.UserID.Value,
                EventDateTime = activeEvent.EventDateTime.Value,
                AuditDateTime = activeEvent.AuditDateTime.Value,
                activeEvent.Reason,
                AccountID = activeEvent.AccountID.Value,
                activeEvent.Active
            },
            AccountDisplayOrderSetEvent displayOrderSetEvent => new
            {
                Type = displayOrderSetEvent.Type,
                EventID = displayOrderSetEvent.EventID.Value,
                UserID = displayOrderSetEvent.UserID.Value,
                EventDateTime = displayOrderSetEvent.EventDateTime.Value,
                AuditDateTime = displayOrderSetEvent.AuditDateTime.Value,
                displayOrderSetEvent.Reason,
                AccountID = displayOrderSetEvent.AccountID.Value,
                DisplayOrder = displayOrderSetEvent.DisplayOrder.Value
            },
            AccountIdentifierSetEvent identifierSetEvent => new
            {
                Type = identifierSetEvent.Type,
                EventID = identifierSetEvent.EventID.Value,
                UserID = identifierSetEvent.UserID.Value,
                EventDateTime = identifierSetEvent.EventDateTime.Value,
                AuditDateTime = identifierSetEvent.AuditDateTime.Value,
                identifierSetEvent.Reason,
                AccountID = identifierSetEvent.AccountID.Value,
                identifierSetEvent.Identifier
            },
            AccountIdentifierUnsetEvent identifierUnsetEvent => new
            {
                Type = identifierUnsetEvent.Type,
                EventID = identifierUnsetEvent.EventID.Value,
                UserID = identifierUnsetEvent.UserID.Value,
                EventDateTime = identifierUnsetEvent.EventDateTime.Value,
                AuditDateTime = identifierUnsetEvent.AuditDateTime.Value,
                identifierUnsetEvent.Reason,
                AccountID = identifierUnsetEvent.AccountID.Value,
                IdentifierType = identifierUnsetEvent.IdentifierType.ToString()
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToBrokerEventResponse(IBrokerEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            BrokerCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                createdEvent.Name,
                LEI = createdEvent.LEI.Value,
                Commission = createdEvent.Commission.Value,
                createdEvent.Active,
                ApprovedDateTime = createdEvent.ApprovedDateTime.Value,
                NextReview = createdEvent.NextReview.Value,
                createdEvent.Notes
            },
            BrokerModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                LEI = modifiedEvent.LEI.Value,
                modifiedEvent.Name,
                Commission = modifiedEvent.Commission.Value
            },
            BrokerActiveSetEvent activeSetEvent => new
            {
                Type = activeSetEvent.Type,
                EventID = activeSetEvent.EventID.Value,
                UserID = activeSetEvent.UserID.Value,
                EventDateTime = activeSetEvent.EventDateTime.Value,
                AuditDateTime = activeSetEvent.AuditDateTime.Value,
                activeSetEvent.Reason,
                LEI = activeSetEvent.LEI.Value,
                activeSetEvent.Active
            },
            BrokerApprovedDateTimeSetEvent approvedDateTimeSetEvent => new
            {
                Type = approvedDateTimeSetEvent.Type,
                EventID = approvedDateTimeSetEvent.EventID.Value,
                UserID = approvedDateTimeSetEvent.UserID.Value,
                EventDateTime = approvedDateTimeSetEvent.EventDateTime.Value,
                AuditDateTime = approvedDateTimeSetEvent.AuditDateTime.Value,
                approvedDateTimeSetEvent.Reason,
                LEI = approvedDateTimeSetEvent.LEI.Value,
                ApprovedDateTime = approvedDateTimeSetEvent.ApprovedDateTime.Value
            },
            BrokerNextReviewSetEvent nextReviewSetEvent => new
            {
                Type = nextReviewSetEvent.Type,
                EventID = nextReviewSetEvent.EventID.Value,
                UserID = nextReviewSetEvent.UserID.Value,
                EventDateTime = nextReviewSetEvent.EventDateTime.Value,
                AuditDateTime = nextReviewSetEvent.AuditDateTime.Value,
                nextReviewSetEvent.Reason,
                LEI = nextReviewSetEvent.LEI.Value,
                NextReview = nextReviewSetEvent.NextReview.Value
            },
            BrokerNotesSetEvent notesSetEvent => new
            {
                Type = notesSetEvent.Type,
                EventID = notesSetEvent.EventID.Value,
                UserID = notesSetEvent.UserID.Value,
                EventDateTime = notesSetEvent.EventDateTime.Value,
                AuditDateTime = notesSetEvent.AuditDateTime.Value,
                notesSetEvent.Reason,
                LEI = notesSetEvent.LEI.Value,
                notesSetEvent.Notes
            },
            BrokerTradeMethodSetEventBase tradeMethodSetEvent => new
            {
                Type = tradeMethodSetEvent.Type,
                EventID = tradeMethodSetEvent.EventID.Value,
                UserID = tradeMethodSetEvent.UserID.Value,
                EventDateTime = tradeMethodSetEvent.EventDateTime.Value,
                AuditDateTime = tradeMethodSetEvent.AuditDateTime.Value,
                tradeMethodSetEvent.Reason,
                LEI = tradeMethodSetEvent.LEI.Value,
                tradeMethodSetEvent.TradeMethod
            },
            BrokerTradeMethodUnsetEvent tradeMethodUnsetEvent => new
            {
                Type = tradeMethodUnsetEvent.Type,
                EventID = tradeMethodUnsetEvent.EventID.Value,
                UserID = tradeMethodUnsetEvent.UserID.Value,
                EventDateTime = tradeMethodUnsetEvent.EventDateTime.Value,
                AuditDateTime = tradeMethodUnsetEvent.AuditDateTime.Value,
                tradeMethodUnsetEvent.Reason,
                LEI = tradeMethodUnsetEvent.LEI.Value,
                tradeMethodUnsetEvent.TradeMethodType
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToCountryEventResponse(ICountryEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            CountryCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                Alpha2 = createdEvent.Alpha2.Value,
                Alpha3 = createdEvent.Alpha3.Value,
                createdEvent.Numeric,
                createdEvent.Name,
                Flag = (CountryFlag?)null
            },
            CountryModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                Alpha2 = modifiedEvent.Alpha2.Value,
                Alpha3 = modifiedEvent.Alpha3.Value,
                modifiedEvent.Numeric,
                modifiedEvent.Name,
                Flag = (CountryFlag?)null
            },
            CountryFlagModifiedEvent flagModifiedEvent => new
            {
                Type = flagModifiedEvent.Type,
                EventID = flagModifiedEvent.EventID.Value,
                UserID = flagModifiedEvent.UserID.Value,
                EventDateTime = flagModifiedEvent.EventDateTime.Value,
                AuditDateTime = flagModifiedEvent.AuditDateTime.Value,
                flagModifiedEvent.Reason,
                Alpha2 = flagModifiedEvent.Alpha2.Value,
                Alpha3 = (string?)null,
                Numeric = (short?)null,
                Name = (string?)null,
                flagModifiedEvent.Flag
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToCurrencyEventResponse(ICurrencyEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            CurrencyCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                AlphabeticCode = createdEvent.AlphabeticCode.Value,
                createdEvent.NumericCode,
                createdEvent.DecimalPlace,
                createdEvent.Name
            },
            CurrencyModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                AlphabeticCode = modifiedEvent.AlphabeticCode.Value,
                modifiedEvent.NumericCode,
                modifiedEvent.DecimalPlace,
                modifiedEvent.Name
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToValuationSettingEventResponse(IValuationSettingEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            AssetAllocationCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                AssetAllocationID = createdEvent.AssetAllocationID.Value,
                createdEvent.Name,
                AccountIDs = createdEvent.AccountIDs.Select(accountID => accountID.Value).ToList(),
                createdEvent.Active,
                RootNodeID = createdEvent.RootNodeID.Value,
                Nodes = ToAssetAllocationNodeResponses(createdEvent.Nodes)
            },
            AssetAllocationModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                AssetAllocationID = modifiedEvent.AssetAllocationID.Value,
                modifiedEvent.Name,
                RootNodeID = modifiedEvent.RootNodeID.Value,
                Nodes = ToAssetAllocationNodeResponses(modifiedEvent.Nodes)
            },
            AssetAllocationAccountIDsSetEvent accountIDsSetEvent => new
            {
                Type = accountIDsSetEvent.Type,
                EventID = accountIDsSetEvent.EventID.Value,
                UserID = accountIDsSetEvent.UserID.Value,
                AuditDateTime = accountIDsSetEvent.AuditDateTime.Value,
                AssetAllocationID = accountIDsSetEvent.AssetAllocationID.Value,
                AccountIDs = accountIDsSetEvent.AccountIDs.Select(accountID => accountID.Value).ToList()
            },
            AssetAllocationActiveSetEvent activeSetEvent => new
            {
                Type = activeSetEvent.Type,
                EventID = activeSetEvent.EventID.Value,
                UserID = activeSetEvent.UserID.Value,
                AuditDateTime = activeSetEvent.AuditDateTime.Value,
                AssetAllocationID = activeSetEvent.AssetAllocationID.Value,
                activeSetEvent.Active
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                AssetAllocationID = @event.AssetAllocationID.Value
            }
        });

    private static List<object> ToAssetAllocationNodeResponses(IEnumerable<AssetAllocationNode> nodes) =>
        nodes
            .Select(node => new
            {
                NodeID = node.NodeID.Value,
                Nodes = node.Nodes.Select(nodeID => nodeID.Value).ToList(),
                node.Name,
                node.Subtotal,
                node.Hidden,
                node.Colour,
                AccountSettings = node.AccountSettings.Select(setting => new
                {
                    AccountID = setting.AccountID.Value,
                    setting.TargetWeight,
                    setting.TargetWeightMax,
                    setting.TargetWeightMin,
                    setting.TargetYield
                }).ToList()
            })
            .Cast<object>()
            .ToList();

    private static object ToAssetAllocationMappingEventResponse(IAssetAllocationMappingEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            UserID = @event.UserID.Value,
            EventDateTime = @event.EventDateTime.Value,
            AuditDateTime = @event.AuditDateTime.Value,
            @event.Reason,
            AssetAllocationID = @event.AssetAllocationID.Value,
            HoldingID = @event.HoldingID.Value,
            NodeID = @event.NodeID.Value
        });

    private static object ToReportEventResponse(IReportEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            ReportCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                ReportID = createdEvent.ReportID.Value,
                createdEvent.Name,
                createdEvent.Active,
                Nodes = ToReportNodeResponses(createdEvent.Nodes)
            },
            ReportModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                ReportID = modifiedEvent.ReportID.Value,
                modifiedEvent.Name,
                modifiedEvent.Active,
                Nodes = ToReportNodeResponses(modifiedEvent.Nodes)
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                ReportID = @event.ReportID.Value
            }
        });

    private static List<object> ToReportNodeResponses(IEnumerable<ReportNodeBase> nodes) =>
        nodes
            .OrderBy(node => node.DisplayOrder)
            .Select(ToReportNodeResponse)
            .ToList();

    private static object ToReportNodeResponse(ReportNodeBase node) =>
        node switch
        {
            ReportNodeChart chart => new
            {
                Type = nameof(ReportNodeChart),
                ReportNodeID = chart.ReportNodeID.Value,
                chart.DisplayOrder,
                chart.Name,
                chart.Title,
                PageOrientation = chart.PageOrientation.ToString(),
                AssetAllocationID = chart.AssetAllocationID.Value,
                ChartType = chart.ChartType.ToString(),
                chart.PieLevel
            },
            ReportNodeValuation valuation => new
            {
                Type = nameof(ReportNodeValuation),
                ReportNodeID = valuation.ReportNodeID.Value,
                valuation.DisplayOrder,
                valuation.Name,
                valuation.Title,
                PageOrientation = valuation.PageOrientation.ToString(),
                AssetAllocationID = valuation.AssetAllocationID.Value,
                Columns = ToReportValuationColumnResponses(valuation.Columns),
                valuation.ColourBullet,
                valuation.ColourText,
                valuation.DisplayHoldings
            },
            ReportNodeTransactions transactions => new
            {
                Type = nameof(ReportNodeTransactions),
                ReportNodeID = transactions.ReportNodeID.Value,
                transactions.DisplayOrder,
                transactions.Name,
                transactions.Title,
                PageOrientation = transactions.PageOrientation.ToString(),
                AssetAllocationID = transactions.AssetAllocationID.Value
            },
            ReportNodeProfitLoss profitLoss => new
            {
                Type = nameof(ReportNodeProfitLoss),
                ReportNodeID = profitLoss.ReportNodeID.Value,
                profitLoss.DisplayOrder,
                profitLoss.Name,
                profitLoss.Title,
                PageOrientation = profitLoss.PageOrientation.ToString(),
                AssetAllocationID = profitLoss.AssetAllocationID.Value,
                ProfitLossMethod = profitLoss.ProfitLossMethod.ToString()
            },
            ReportNodeCash cash => new
            {
                Type = nameof(ReportNodeCash),
                ReportNodeID = cash.ReportNodeID.Value,
                cash.DisplayOrder,
                cash.Name,
                cash.Title,
                PageOrientation = cash.PageOrientation.ToString(),
                AssetAllocationID = cash.AssetAllocationID.Value
            },
            _ => new
            {
                Type = node.GetType().Name,
                ReportNodeID = node.ReportNodeID.Value,
                node.DisplayOrder,
                node.Name,
                node.Title,
                PageOrientation = node.PageOrientation.ToString()
            }
        };

    private static List<object> ToReportValuationColumnResponses(IEnumerable<ReportValuationColumn>? columns) =>
        ReportConfigBuilder.NormaliseValuationColumns(columns)
            .Select(column => new
            {
                ColumnKey = column.ColumnKey.ToString(),
                column.DisplayOrder
            })
            .Cast<object>()
            .ToList();

    private static object ToHoldingEventResponse(IHoldingEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            HoldingCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                HoldingID = createdEvent.HoldingID.Value,
                AccountID = createdEvent.AccountID.Value,
                InstrumentID = createdEvent.InstrumentID.Value,
                HoldingKind = createdEvent.GetHoldingKindName(),
                createdEvent.Name,
                createdEvent.Active,
                createdEvent.Default,
                BankName = createdEvent is HoldingCashBaseCreatedEvent bankEvent ? bankEvent.BankName : null,
                AccountName = createdEvent is HoldingCashBaseCreatedEvent accountEvent ? accountEvent.AccountName : null,
                SortCode = createdEvent is HoldingCashBaseCreatedEvent sortEvent ? sortEvent.SortCode.Value : null,
                AccountNumber = createdEvent is HoldingCashBaseCreatedEvent numberEvent ? numberEvent.AccountNumber.Value : null,
                BIC = createdEvent is HoldingCashBaseCreatedEvent bicEvent ? bicEvent.BIC.Value : null,
                IBAN = createdEvent is HoldingCashBaseCreatedEvent ibanEvent ? ibanEvent.IBAN.Value : null
            },
            HoldingModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                HoldingID = modifiedEvent.HoldingID.Value,
                HoldingKind = modifiedEvent.GetHoldingKindName(),
                modifiedEvent.Name,
                modifiedEvent.Default,
                BankName = modifiedEvent is HoldingCashBaseModifiedEvent bankEvent ? bankEvent.BankName : null,
                AccountName = modifiedEvent is HoldingCashBaseModifiedEvent accountEvent ? accountEvent.AccountName : null,
                SortCode = modifiedEvent is HoldingCashBaseModifiedEvent sortEvent ? sortEvent.SortCode.Value : null,
                AccountNumber = modifiedEvent is HoldingCashBaseModifiedEvent numberEvent ? numberEvent.AccountNumber.Value : null,
                BIC = modifiedEvent is HoldingCashBaseModifiedEvent bicEvent ? bicEvent.BIC.Value : null,
                IBAN = modifiedEvent is HoldingCashBaseModifiedEvent ibanEvent ? ibanEvent.IBAN.Value : null
            },
            HoldingActiveModifiedEvent activeEvent => new
            {
                Type = activeEvent.Type,
                EventID = activeEvent.EventID.Value,
                UserID = activeEvent.UserID.Value,
                EventDateTime = activeEvent.EventDateTime.Value,
                AuditDateTime = activeEvent.AuditDateTime.Value,
                activeEvent.Reason,
                HoldingID = activeEvent.HoldingID.Value,
                activeEvent.Active
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToFXEventResponse(IFXEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            FXCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                Pair = createdEvent.Pair.Value,
                DisplayPair = createdEvent.Pair.DisplayValue,
                BaseCurrency = createdEvent.BaseCurrency.Value,
                QuoteCurrency = createdEvent.QuoteCurrency.Value,
                createdEvent.Active
            },
            FXActiveModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                Pair = modifiedEvent.Pair.Value,
                DisplayPair = modifiedEvent.Pair.DisplayValue,
                BaseCurrency = modifiedEvent.Pair.BaseCurrency.Value,
                QuoteCurrency = modifiedEvent.Pair.QuoteCurrency.Value,
                modifiedEvent.Active
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToFXRateEventResponse(IFXRateEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            FXRateSetEvent setEvent => new
            {
                Type = setEvent.Type,
                EventID = setEvent.EventID.Value,
                UserID = setEvent.UserID.Value,
                EventDateTime = setEvent.EventDateTime.Value,
                AuditDateTime = setEvent.AuditDateTime.Value,
                setEvent.Reason,
                Pair = setEvent.Pair.Value,
                DisplayPair = setEvent.Pair.DisplayValue,
                setEvent.Price
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToInstrumentEventResponse(IInstrumentEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            InstrumentCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                InstrumentID = createdEvent.InstrumentID.Value,
                createdEvent.Name,
                createdEvent.FormalName,
                Exchange = createdEvent.Exchange.Value,
                CFI = createdEvent.CFI.Value,
                createdEvent.Logo,
                createdEvent.Active,
                IncomeCountry = createdEvent.IncomeCountry.Value,
                PriceCountry = createdEvent.PriceCountry.Value,
                PriceCurrency = createdEvent.PriceCurrency.Value
            },
            InstrumentModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                InstrumentID = modifiedEvent.InstrumentID.Value,
                modifiedEvent.Name,
                modifiedEvent.FormalName,
                Exchange = modifiedEvent.Exchange.Value,
                CFI = modifiedEvent.CFI.Value,
                modifiedEvent.Logo,
                IncomeCountry = modifiedEvent.IncomeCountry.Value,
                PriceCountry = modifiedEvent.PriceCountry.Value,
                PriceCurrency = modifiedEvent.PriceCurrency.Value
            },
            InstrumentActiveModifiedEvent activeEvent => new
            {
                Type = activeEvent.Type,
                EventID = activeEvent.EventID.Value,
                UserID = activeEvent.UserID.Value,
                EventDateTime = activeEvent.EventDateTime.Value,
                AuditDateTime = activeEvent.AuditDateTime.Value,
                activeEvent.Reason,
                InstrumentID = activeEvent.InstrumentID.Value,
                activeEvent.Active
            },
            InstrumentIdentifierSetEvent setEvent => new
            {
                Type = setEvent.Type,
                EventID = setEvent.EventID.Value,
                UserID = setEvent.UserID.Value,
                EventDateTime = setEvent.EventDateTime.Value,
                AuditDateTime = setEvent.AuditDateTime.Value,
                setEvent.Reason,
                InstrumentID = setEvent.InstrumentID.Value,
                setEvent.Identifier
            },
            InstrumentIdentifierUnsetEvent unsetEvent => new
            {
                Type = unsetEvent.Type,
                EventID = unsetEvent.EventID.Value,
                UserID = unsetEvent.UserID.Value,
                EventDateTime = unsetEvent.EventDateTime.Value,
                AuditDateTime = unsetEvent.AuditDateTime.Value,
                unsetEvent.Reason,
                InstrumentID = unsetEvent.InstrumentID.Value,
                unsetEvent.IdentifierType
            },
            InstrumentTermsSetEvent termsEvent => new
            {
                Type = termsEvent.Type,
                EventID = termsEvent.EventID.Value,
                UserID = termsEvent.UserID.Value,
                EventDateTime = termsEvent.EventDateTime.Value,
                AuditDateTime = termsEvent.AuditDateTime.Value,
                termsEvent.Reason,
                InstrumentID = termsEvent.InstrumentID.Value,
                termsEvent.Terms
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToInstrumentPriceEventResponse(IInstrumentPriceEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            InstrumentPriceSetEvent setEvent => new
            {
                Type = setEvent.Type,
                EventID = setEvent.EventID.Value,
                UserID = setEvent.UserID.Value,
                EventDateTime = setEvent.EventDateTime.Value,
                AuditDateTime = setEvent.AuditDateTime.Value,
                setEvent.Reason,
                InstrumentID = setEvent.InstrumentID.Value,
                setEvent.Price
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToInstrumentIncomeEventResponse(IInstrumentIncomeEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            InstrumentIncomeSetEvent setEvent => new
            {
                Type = setEvent.Type,
                EventID = setEvent.EventID.Value,
                UserID = setEvent.UserID.Value,
                EventDateTime = setEvent.EventDateTime.Value,
                AuditDateTime = setEvent.AuditDateTime.Value,
                setEvent.Reason,
                InstrumentID = setEvent.InstrumentID.Value,
                setEvent.Income
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToTransactionEventResponse(ITransactionEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            TransactionCreditEvent creditEvent => new
            {
                Type = creditEvent.Type,
                EventID = creditEvent.EventID.Value,
                UserID = creditEvent.UserID.Value,
                EventDateTime = creditEvent.EventDateTime.Value,
                SettlementDateTime = creditEvent.SettlementDateTime.Value,
                AuditDateTime = creditEvent.AuditDateTime.Value,
                creditEvent.Reason,
                EventSetID = creditEvent.EventSetID.Value,
                EventIDGroup = creditEvent.EventIDGroup.Select(eventId => eventId.Value).ToList(),
                HoldingID = creditEvent.HoldingID?.Value,
                InstrumentID = creditEvent.InstrumentID.Value,
                AccountID = creditEvent.AccountID.Value,
                Quantity = creditEvent.Quantity.Value,
                LocalCost = creditEvent.LocalCost.Value,
                LocalCostCurrency = creditEvent.LocalCostCurrency.Value,
                BookCost = creditEvent.BookCost.Value,
                BookCostSource = creditEvent.BookCostSource.ToString(),
                creditEvent.BookCostEstimated
            },
            TransactionDebitEvent debitEvent => new
            {
                Type = debitEvent.Type,
                EventID = debitEvent.EventID.Value,
                UserID = debitEvent.UserID.Value,
                EventDateTime = debitEvent.EventDateTime.Value,
                SettlementDateTime = debitEvent.SettlementDateTime.Value,
                AuditDateTime = debitEvent.AuditDateTime.Value,
                debitEvent.Reason,
                EventSetID = debitEvent.EventSetID.Value,
                EventIDGroup = debitEvent.EventIDGroup.Select(eventId => eventId.Value).ToList(),
                HoldingID = debitEvent.HoldingID?.Value,
                InstrumentID = debitEvent.InstrumentID.Value,
                AccountID = debitEvent.AccountID.Value,
                Quantity = debitEvent.Quantity.Value,
                LocalCost = debitEvent.LocalCost.Value,
                LocalCostCurrency = debitEvent.LocalCostCurrency.Value,
                BookCost = debitEvent.BookCost.Value,
                BookCostSource = debitEvent.BookCostSource.ToString(),
                debitEvent.BookCostEstimated
            },
            TransactionBookCostAdjustedEvent adjustmentEvent => new
            {
                Type = adjustmentEvent.Type,
                EventID = adjustmentEvent.EventID.Value,
                UserID = adjustmentEvent.UserID.Value,
                EventDateTime = adjustmentEvent.EventDateTime.Value,
                SettlementDateTime = adjustmentEvent.SettlementDateTime.Value,
                AuditDateTime = adjustmentEvent.AuditDateTime.Value,
                adjustmentEvent.Reason,
                EventSetID = adjustmentEvent.EventSetID.Value,
                AdjustedIDGroup = adjustmentEvent.AdjustedIDGroup.Select(eventId => eventId.Value).ToList(),
                AccountID = adjustmentEvent.AccountID.Value,
                BookCost = adjustmentEvent.BookCost.Value,
                BookCostSource = adjustmentEvent.BookCostSource.ToString(),
                adjustmentEvent.BookCostEstimated
            },
            TransactionCancellationEvent cancellationEvent => new
            {
                Type = cancellationEvent.Type,
                EventID = cancellationEvent.EventID.Value,
                UserID = cancellationEvent.UserID.Value,
                EventDateTime = cancellationEvent.EventDateTime.Value,
                SettlementDateTime = cancellationEvent.SettlementDateTime.Value,
                AuditDateTime = cancellationEvent.AuditDateTime.Value,
                cancellationEvent.Reason,
                EventSetID = cancellationEvent.EventSetID.Value,
                EventIDGroup = cancellationEvent.EventIDGroup.Select(eventId => eventId.Value).ToList(),
                AccountID = cancellationEvent.AccountID?.Value,
                CancelledEventID = cancellationEvent.CancelledEventID.Value,
                CancelledIDGroup = cancellationEvent.CancelledIDGroup.Select(eventId => eventId.Value).ToList()
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToTicketEventResponse(ITicket @event) =>
        @event switch
        {
            TicketCreatedEvent createdEvent => WithTicketBase(createdEvent, new
            {
                Side = createdEvent.Side.ToString(),
                InstrumentID = createdEvent.InstrumentID.Value,
                TradeCurrency = createdEvent.TradeCurrency.Value
            }),
            TicketAccountAddedEvent accountAddedEvent => WithTicketBase(accountAddedEvent, new
            {
                AccountID = accountAddedEvent.AccountID.Value
            }),
            TicketAccountRemovedEvent accountRemovedEvent => WithTicketBase(accountRemovedEvent, new
            {
                AccountID = accountRemovedEvent.AccountID.Value
            }),
            TicketProposalCreatedEvent proposalCreatedEvent => WithTicketBase(proposalCreatedEvent, new
            {
                proposalCreatedEvent.TargetPrice,
                TradeCurrency = proposalCreatedEvent.TradeCurrency.Value,
                Allocations = proposalCreatedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketProposalModifiedEvent proposalModifiedEvent => WithTicketBase(proposalModifiedEvent, new
            {
                proposalModifiedEvent.TargetPrice,
                TradeCurrency = proposalModifiedEvent.TradeCurrency.Value,
                Allocations = proposalModifiedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketProposalReasonSetEvent proposalReasonSetEvent => WithTicketBase(proposalReasonSetEvent, new
            {
                proposalReasonSetEvent.ProposalReason
            }),
            TicketProposalAllocationSetEvent proposalAllocationSetEvent => WithTicketBase(proposalAllocationSetEvent, new
            {
                proposalAllocationSetEvent.ProposalAllocation
            }),
            TicketTradeCreatedEvent tradeCreatedEvent => WithTicketBase(tradeCreatedEvent, new
            {
                tradeCreatedEvent.TradedPrice,
                tradeCreatedEvent.TradeDateTime,
                tradeCreatedEvent.SettlementDateTime,
                Allocations = tradeCreatedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketTradeModifiedEvent tradeModifiedEvent => WithTicketBase(tradeModifiedEvent, new
            {
                tradeModifiedEvent.TradedPrice,
                tradeModifiedEvent.TradeDateTime,
                tradeModifiedEvent.SettlementDateTime,
                Allocations = tradeModifiedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketTradeFillAddedEvent fillAddedEvent => WithTicketBase(fillAddedEvent, new
            {
                fillAddedEvent.FillID,
                fillAddedEvent.BrokerLEI,
                fillAddedEvent.Price,
                fillAddedEvent.Quantity,
                fillAddedEvent.SettlementAmount,
                fillAddedEvent.BookCostOverride,
                fillAddedEvent.Note
            }),
            TicketTradeFillModifiedEvent fillModifiedEvent => WithTicketBase(fillModifiedEvent, new
            {
                fillModifiedEvent.FillID,
                fillModifiedEvent.BrokerLEI,
                fillModifiedEvent.Price,
                fillModifiedEvent.Quantity,
                fillModifiedEvent.SettlementAmount,
                fillModifiedEvent.BookCostOverride,
                fillModifiedEvent.Note
            }),
            TicketTradeFillRemovedEvent fillRemovedEvent => WithTicketBase(fillRemovedEvent, new
            {
                fillRemovedEvent.FillID
            }),
            TicketTradeDecisionRequestedEvent tradeDecisionRequestedEvent => WithTicketBase(tradeDecisionRequestedEvent, new { }),
            TicketTradeInstructionNotesSetEvent tradeInstructionNotesSetEvent => WithTicketBase(tradeInstructionNotesSetEvent, new
            {
                tradeInstructionNotesSetEvent.TradeInstructionNotes
            }),
            TicketTradeProgressNotesSetEvent tradeProgressNotesSetEvent => WithTicketBase(tradeProgressNotesSetEvent, new
            {
                tradeProgressNotesSetEvent.TradeProgressNotes
            }),
            _ => WithTicketBase(@event, new { })
        };

    private static object WithTicketBase(ITicket @event, object details)
    {
        var response = new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            UserID = @event.UserID.Value,
            EventDateTime = @event.EventDateTime.Value,
            AuditDateTime = @event.AuditDateTime.Value,
            @event.Reason,
            TicketNumber = @event.TicketNumber.Value,
            Details = details
        };

        return EventPropertyDetailsFactory.WithPropertyDetails(@event, response, details);
    }

    private static object ToResponse(TicketProposalAllocation allocation) =>
        new
        {
            AccountID = allocation.AccountID.Value,
            allocation.Quantity
        };

    private static object ToResponse(TicketTradeAllocation allocation) =>
        new
        {
            AccountID = allocation.AccountID.Value,
            CashHoldingID = allocation.CashHoldingID?.Value,
            allocation.Quantity,
            allocation.SettlementAmount,
            allocation.BookCostOverride
        };
}
