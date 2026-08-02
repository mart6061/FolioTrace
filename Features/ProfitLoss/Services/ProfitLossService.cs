using System.Text.Json;
using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Snapshots;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class ProfitLossService(
    IEventRepository eventRepository,
    AccountService accountService,
    HoldingService holdingService,
    InstrumentService instrumentService,
    InstrumentValueService instrumentValueService,
    FXRateService fxRateService,
    IAggregateSnapshotRepository snapshotRepository,
    int cacheCapacity = 2000,
    ProfitLossSnapshotVerificationOptions? verificationOptions = null)
{
    private const string AggregateKind = "ProfitLoss";
    private readonly BoundedLruCache<ProfitLossCacheKey, ProfitLosses> cache = new(cacheCapacity);
    private readonly Lock cacheLock = new();
    private readonly ProfitLossSnapshotVerificationOptions verification = verificationOptions ?? new();
    private readonly Lock verificationLock = new();
    private int verifiedCount;
    private int mismatchCount;
    private DateTime? lastMismatchAtUtc;
    private string? lastMismatchDetails;

    public ProfitLossServiceDiagnostics GetDiagnostics()
    {
        int cacheEntryCount;
        int holdingCount;
        long estimatedMemoryBytes;
        lock (cacheLock)
        {
            cacheEntryCount = cache.Count;
            holdingCount = cache.Values
                .OrderByDescending(result => result.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Accounts.Sum(account => account.Items.Count) ?? 0;
            estimatedMemoryBytes = CacheMemoryEstimator.EstimateBytes(cache.Values);
        }

        lock (verificationLock)
        {
            return new ProfitLossServiceDiagnostics(
                cacheEntryCount,
                holdingCount,
                estimatedMemoryBytes,
                verifiedCount,
                mismatchCount,
                lastMismatchAtUtc,
                lastMismatchDetails);
        }
    }

    public bool IsCached(EventDateTime valuationDate, HoldingDateBasis holdingDateBasis) =>
        IsCached(ProfitLossCacheKey.Current(valuationDate, holdingDateBasis, InstrumentPriceBasis.Mid, null, null));

    public int Invalidate(IEventBase @event)
    {
        var invalidationDate = GetInvalidationDate(@event);
        var removed = InvalidateFrom(invalidationDate);

        if (@event is ITransactionEvent)
        {
            foreach (HoldingDateBasis basis in Enum.GetValues<HoldingDateBasis>())
                snapshotRepository.RetireFromAsync(AggregateKind, Constants.Initialisation.TransactionsStreamId, invalidationDate, basis.ToString()).GetAwaiter().GetResult();
        }

        return removed;
    }

    public int InvalidateAll()
    {
        lock (cacheLock)
        {
            var removed = cache.Count;
            cache.Clear();
            return removed;
        }
    }

    public Task<ProfitLosses> Get(
        EventDateTime valuationDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis = InstrumentPriceBasis.Mid,
        AccountID? accountID = null) =>
        GetCurrent(valuationDateTime, holdingDateBasis, instrumentPriceBasis, accountID, null);

    public Task<ProfitLosses> Get(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        AccountID? accountID = null) =>
        GetHistorical(valuationDateTime, asOfDateTime, holdingDateBasis, instrumentPriceBasis, accountID, null);

    public async Task<HoldingProfitLossDetails?> GetHolding(
        EventDateTime valuationDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        HoldingID holdingID)
    {
        var result = await GetCurrent(valuationDateTime, holdingDateBasis, instrumentPriceBasis, null, holdingID);
        return CreateHoldingDetails(result, holdingID);
    }

    public async Task<HoldingProfitLossDetails?> GetHolding(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        HoldingID holdingID)
    {
        var result = await GetHistorical(valuationDateTime, asOfDateTime, holdingDateBasis, instrumentPriceBasis, null, holdingID);
        return CreateHoldingDetails(result, holdingID);
    }

    public async Task PersistSnapshotAsync(EventDateTime valuationDate, HoldingDateBasis holdingDateBasis, CancellationToken cancellationToken = default)
    {
        var transactionEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, cancellationToken);
        if (transactionEvents.Count == 0)
            return;

        var orderedEvents = transactionEvents
            .Where(@event => @event.EventDateTime.Value <= valuationDate.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();
        if (orderedEvents.Count == 0)
            return;
        var latest = orderedEvents[^1];
        var payload = new ProfitLossSnapshotPayload(orderedEvents
            .Select(@event => new ProfitLossSnapshotEvent(
                @event.Type,
                JsonSerializer.Serialize(@event, @event.GetType())))
            .ToList());

        await snapshotRepository.SaveAsync(new AggregateSnapshot
        {
            Id = Guid.CreateGuid7(),
            AggregateKind = AggregateKind,
            StreamId = Constants.Initialisation.TransactionsStreamId,
            Variant = holdingDateBasis.ToString(),
            ValuationDateTime = valuationDate.Value,
            AsOfDateTime = DateTime.UtcNow,
            LastEventID = latest.EventID.Value,
            LastAuditDateTime = latest.AuditDateTime.Value,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = orderedEvents.Count
        }, cancellationToken);
    }

    private async Task<ProfitLosses> GetCurrent(
        EventDateTime valuationDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        AccountID? accountID,
        HoldingID? holdingID)
    {
        var key = ProfitLossCacheKey.Current(valuationDateTime, holdingDateBasis, instrumentPriceBasis, accountID, holdingID);
        if (TryGetCached(key, out var cached))
            return cached;

        var asOfDateTime = AuditDateTimeBuilder.Create();
        var snapshot = await snapshotRepository.FindLatestAsync(
            AggregateKind,
            Constants.Initialisation.TransactionsStreamId,
            valuationDateTime.Value,
            holdingDateBasis.ToString());

        IReadOnlyList<ITransactionEvent> transactionEvents;
        if (snapshot is null)
        {
            transactionEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId);
        }
        else
        {
            var payload = JsonSerializer.Deserialize<ProfitLossSnapshotPayload>(snapshot.PayloadJson)
                ?? throw new InvalidOperationException("The Profit/Loss snapshot payload could not be read.");
            var delta = await eventRepository.LoadStreamAfterAsync<ITransactionEvent>(
                Constants.Initialisation.TransactionsStreamId,
                new EventID(snapshot.LastEventID));
            transactionEvents = payload.Events.Select(DeserializeSnapshotEvent).Concat(delta).ToList();
        }

        var result = await Build(valuationDateTime, asOfDateTime, holdingDateBasis, instrumentPriceBasis, accountID, holdingID, transactionEvents);
        SetCached(key, result);

        if (snapshot is not null && verification.Enabled && Random.Shared.NextDouble() < verification.SampleRate)
            await VerifySnapshotAsync(result, valuationDateTime, asOfDateTime, holdingDateBasis, instrumentPriceBasis, accountID, holdingID);

        return result;
    }

    private async Task<ProfitLosses> GetHistorical(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        AccountID? accountID,
        HoldingID? holdingID)
    {
        var key = ProfitLossCacheKey.Historical(valuationDateTime, asOfDateTime, holdingDateBasis, instrumentPriceBasis, accountID, holdingID);
        if (TryGetCached(key, out var cached))
            return cached;

        var transactionEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId);
        var result = await Build(valuationDateTime, asOfDateTime, holdingDateBasis, instrumentPriceBasis, accountID, holdingID, transactionEvents);
        SetCached(key, result);
        return result;
    }

    private async Task<ProfitLosses> Build(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        AccountID? accountID,
        HoldingID? holdingID,
        IReadOnlyList<ITransactionEvent> transactionEvents)
    {
        var accountsTask = accountService.Get(valuationDateTime, asOfDateTime);
        var holdingsTask = holdingService.Get(valuationDateTime, asOfDateTime);
        var instrumentsTask = instrumentService.Get(valuationDateTime, asOfDateTime);
        var instrumentValuesTask = instrumentValueService.Get(valuationDateTime, asOfDateTime);
        var fxRatesTask = fxRateService.Get(valuationDateTime, asOfDateTime);
        await Task.WhenAll(accountsTask, holdingsTask, instrumentsTask, instrumentValuesTask, fxRatesTask);

        return new ProfitLosses(
            valuationDateTime,
            asOfDateTime,
            holdingDateBasis,
            await accountsTask,
            await holdingsTask,
            await instrumentsTask,
            await instrumentValuesTask,
            await fxRatesTask,
            transactionEvents,
            instrumentPriceBasis,
            accountID,
            holdingID);
    }

    private async Task VerifySnapshotAsync(
        ProfitLosses seeded,
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        AccountID? accountID,
        HoldingID? holdingID)
    {
        var events = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId);
        var replay = await Build(valuationDateTime, asOfDateTime, holdingDateBasis, instrumentPriceBasis, accountID, holdingID, events);
        var mismatch = JsonSerializer.Serialize(seeded.Accounts) == JsonSerializer.Serialize(replay.Accounts)
            ? null
            : $"Snapshot and full replay differ for {valuationDateTime.Value:O} {holdingDateBasis}.";

        lock (verificationLock)
        {
            verifiedCount++;
            if (mismatch is not null)
            {
                mismatchCount++;
                lastMismatchAtUtc = DateTime.UtcNow;
                lastMismatchDetails = mismatch;
            }
        }
    }

    private static HoldingProfitLossDetails? CreateHoldingDetails(ProfitLosses result, HoldingID holdingID)
    {
        var account = result.Accounts.FirstOrDefault(account => account.Items.Any(item => item.HoldingID == holdingID));
        var item = account?.Items.FirstOrDefault(item => item.HoldingID == holdingID);
        if (account is null || item is null)
            return null;

        return new HoldingProfitLossDetails
        {
            AccountID = account.AccountID,
            Currency = account.BookCurrency,
            HoldingID = item.HoldingID,
            HoldingName = item.HoldingName,
            InstrumentName = item.InstrumentName,
            DefaultMethod = account.DefaultMethod,
            Methods = item.Methods,
            Rows = item.Rows
        };
    }

    private bool IsCached(ProfitLossCacheKey key)
    {
        lock (cacheLock)
            return cache.ContainsKey(key);
    }

    private bool TryGetCached(ProfitLossCacheKey key, out ProfitLosses result)
    {
        lock (cacheLock)
            return cache.TryGetValue(key, out result!);
    }

    private void SetCached(ProfitLossCacheKey key, ProfitLosses result)
    {
        lock (cacheLock)
            cache[key] = result;
    }

    private int InvalidateFrom(DateTime eventDateTime)
    {
        lock (cacheLock)
        {
            var removed = 0;
            foreach (var key in cache.Keys.Where(key => key.ValuationDateTime >= eventDateTime).ToList())
            {
                if (cache.Remove(key))
                    removed++;
            }
            return removed;
        }
    }

    private static DateTime GetInvalidationDate(IEventBase @event) =>
        @event is ITransactionEvent transaction
            ? new[] { transaction.EventDateTime.Value, transaction.SettlementDateTime.Value }.Min()
            : @event.EventDateTime.Value;

    private static ITransactionEvent DeserializeSnapshotEvent(ProfitLossSnapshotEvent snapshotEvent) =>
        snapshotEvent.Type switch
        {
            nameof(TransactionCreditEvent) => (ITransactionEvent?)JsonSerializer.Deserialize<TransactionCreditEvent>(snapshotEvent.Json),
            nameof(TransactionDebitEvent) => JsonSerializer.Deserialize<TransactionDebitEvent>(snapshotEvent.Json),
            nameof(TransactionBookCostAdjustedEvent) => JsonSerializer.Deserialize<TransactionBookCostAdjustedEvent>(snapshotEvent.Json),
            nameof(TransactionCancellationEvent) => JsonSerializer.Deserialize<TransactionCancellationEvent>(snapshotEvent.Json),
            _ => throw new InvalidOperationException($"Unsupported transaction snapshot event type '{snapshotEvent.Type}'.")
        } ?? throw new InvalidOperationException($"Transaction snapshot event '{snapshotEvent.Type}' could not be read.");

    private sealed record ProfitLossSnapshotPayload(IReadOnlyList<ProfitLossSnapshotEvent> Events);

    private sealed record ProfitLossSnapshotEvent(string Type, string Json);

    private readonly record struct ProfitLossCacheKey(
        DateTime ValuationDateTime,
        DateTime? AsOfDateTime,
        HoldingDateBasis HoldingDateBasis,
        InstrumentPriceBasis InstrumentPriceBasis,
        Guid? AccountID,
        Guid? HoldingID)
    {
        public static ProfitLossCacheKey Current(EventDateTime valuationDate, HoldingDateBasis holdingDateBasis, InstrumentPriceBasis instrumentPriceBasis, AccountID? accountID, HoldingID? holdingID) =>
            new(valuationDate.Value, null, holdingDateBasis, instrumentPriceBasis, accountID?.Value, holdingID?.Value);

        public static ProfitLossCacheKey Historical(EventDateTime valuationDate, AuditDateTime asOfDateTime, HoldingDateBasis holdingDateBasis, InstrumentPriceBasis instrumentPriceBasis, AccountID? accountID, HoldingID? holdingID) =>
            new(valuationDate.Value, asOfDateTime.Value, holdingDateBasis, instrumentPriceBasis, accountID?.Value, holdingID?.Value);
    }
}
