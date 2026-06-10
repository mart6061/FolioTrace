using FolioTrace.Aggregates;
using Microsoft.Extensions.DependencyInjection;

namespace Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolioTraceServices(this IServiceCollection services)
    {
        services.AddSingleton<AccountService>();
        services.AddSingleton<BrokerService>();
        services.AddSingleton<CountryService>();
        services.AddSingleton<CurrencyService>();
        services.AddSingleton<FXService>();
        services.AddSingleton<FXRateService>();
        services.AddSingleton<FoleoTraderOrderService>();
        services.AddSingleton<HoldingService>();
        services.AddSingleton<HoldingPositionService>();
        services.AddSingleton<InstrumentService>();
        services.AddSingleton<InstrumentValueService>();
        services.AddSingleton<ValuationService>();
        services.AddSingleton<TicketService>();
        services.AddSingleton<UserService>();
        services.AddSingleton<UserMenuPreferencesService>();
        services.AddSingleton<UserValuationPreferencesService>();
        services.AddSingleton<UserBookmarksService>();
        services.AddSingleton<ValuationSettingService>();
        services.AddSingleton<AggregateMaintenanceCoordinator>();
        services.AddSingleton<AggregateUpdateNotificationService>();
        services.AddSingleton<IAggregateCacheInvalidator<AccountCreatedEvent>>(provider => new AggregateCacheInvalidator<AccountCreatedEvent>(@event =>
            provider.GetRequiredService<AccountService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<AccountModifiedEvent>>(provider => new AggregateCacheInvalidator<AccountModifiedEvent>(@event =>
            provider.GetRequiredService<AccountService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<AccountActiveSetEvent>>(provider => new AggregateCacheInvalidator<AccountActiveSetEvent>(@event =>
            provider.GetRequiredService<AccountService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<AccountDisplayOrderSetEvent>>(provider => new AggregateCacheInvalidator<AccountDisplayOrderSetEvent>(@event =>
            provider.GetRequiredService<AccountService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<IBrokerEvent>>(provider => new AggregateCacheInvalidator<IBrokerEvent>(provider.GetRequiredService<BrokerService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CountryCreatedEvent>>(provider => new AggregateCacheInvalidator<CountryCreatedEvent>(provider.GetRequiredService<CountryService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CountryModifiedEvent>>(provider => new AggregateCacheInvalidator<CountryModifiedEvent>(provider.GetRequiredService<CountryService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CountryFlagModifiedEvent>>(provider => new AggregateCacheInvalidator<CountryFlagModifiedEvent>(provider.GetRequiredService<CountryService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CurrencyCreatedEvent>>(provider => new AggregateCacheInvalidator<CurrencyCreatedEvent>(provider.GetRequiredService<CurrencyService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CurrencyModifiedEvent>>(provider => new AggregateCacheInvalidator<CurrencyModifiedEvent>(provider.GetRequiredService<CurrencyService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<FXCreatedEvent>>(provider => new AggregateCacheInvalidator<FXCreatedEvent>(@event =>
            provider.GetRequiredService<FXService>().Invalidate(@event) + provider.GetRequiredService<FXRateService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<FXActiveModifiedEvent>>(provider => new AggregateCacheInvalidator<FXActiveModifiedEvent>(@event =>
            provider.GetRequiredService<FXService>().Invalidate(@event) + provider.GetRequiredService<FXRateService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<FXRateSetEvent>>(provider => new AggregateCacheInvalidator<FXRateSetEvent>(provider.GetRequiredService<FXRateService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<IFoleoTraderOrderEvent>>(provider => new AggregateCacheInvalidator<IFoleoTraderOrderEvent>(provider.GetRequiredService<FoleoTraderOrderService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<IHoldingEvent>>(provider => new AggregateCacheInvalidator<IHoldingEvent>(@event =>
            provider.GetRequiredService<HoldingService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<HoldingActiveModifiedEvent>>(provider => new AggregateCacheInvalidator<HoldingActiveModifiedEvent>(@event =>
            provider.GetRequiredService<HoldingService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<InstrumentCreatedEvent>>(provider => new AggregateCacheInvalidator<InstrumentCreatedEvent>(@event =>
            provider.GetRequiredService<InstrumentService>().Invalidate(@event) + provider.GetRequiredService<InstrumentValueService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<InstrumentModifiedEvent>>(provider => new AggregateCacheInvalidator<InstrumentModifiedEvent>(@event =>
            provider.GetRequiredService<InstrumentService>().Invalidate(@event) + provider.GetRequiredService<InstrumentValueService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<InstrumentActiveModifiedEvent>>(provider => new AggregateCacheInvalidator<InstrumentActiveModifiedEvent>(@event =>
            provider.GetRequiredService<InstrumentService>().Invalidate(@event) + provider.GetRequiredService<InstrumentValueService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<InstrumentIdentifierSetEvent>>(provider => new AggregateCacheInvalidator<InstrumentIdentifierSetEvent>(@event =>
            provider.GetRequiredService<InstrumentService>().Invalidate(@event) + provider.GetRequiredService<InstrumentValueService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<InstrumentIdentifierUnsetEvent>>(provider => new AggregateCacheInvalidator<InstrumentIdentifierUnsetEvent>(@event =>
            provider.GetRequiredService<InstrumentService>().Invalidate(@event) + provider.GetRequiredService<InstrumentValueService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<InstrumentTermsSetEvent>>(provider => new AggregateCacheInvalidator<InstrumentTermsSetEvent>(@event =>
            provider.GetRequiredService<InstrumentService>().Invalidate(@event) + provider.GetRequiredService<InstrumentValueService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<InstrumentPriceSetEvent>>(provider => new AggregateCacheInvalidator<InstrumentPriceSetEvent>(provider.GetRequiredService<InstrumentValueService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<InstrumentIncomeSetEvent>>(provider => new AggregateCacheInvalidator<InstrumentIncomeSetEvent>(provider.GetRequiredService<InstrumentValueService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<TransactionCreditEvent>>(provider => new AggregateCacheInvalidator<TransactionCreditEvent>(provider.GetRequiredService<HoldingPositionService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<TransactionDebitEvent>>(provider => new AggregateCacheInvalidator<TransactionDebitEvent>(provider.GetRequiredService<HoldingPositionService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<TransactionCancellationEvent>>(provider => new AggregateCacheInvalidator<TransactionCancellationEvent>(provider.GetRequiredService<HoldingPositionService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<ITicket>>(provider => new AggregateCacheInvalidator<ITicket>(provider.GetRequiredService<TicketService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<IUserEvent>>(provider => new AggregateCacheInvalidator<IUserEvent>(provider.GetRequiredService<UserService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<IUserMenuPreferencesEvent>>(provider => new AggregateCacheInvalidator<IUserMenuPreferencesEvent>(provider.GetRequiredService<UserMenuPreferencesService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<IUserValuationPreferencesEvent>>(provider => new AggregateCacheInvalidator<IUserValuationPreferencesEvent>(provider.GetRequiredService<UserValuationPreferencesService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<IUserBookmarksEvent>>(provider => new AggregateCacheInvalidator<IUserBookmarksEvent>(provider.GetRequiredService<UserBookmarksService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<IValuationSettingEvent>>(provider => new AggregateCacheInvalidator<IValuationSettingEvent>(provider.GetRequiredService<ValuationSettingService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<AccountCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<AccountModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<AccountActiveSetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<AccountDisplayOrderSetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<IBrokerEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryFlagModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CurrencyCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CurrencyModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<FXCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<FXActiveModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<FXRateSetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<IFoleoTraderOrderEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<IHoldingEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<InstrumentCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<InstrumentModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<InstrumentActiveModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<InstrumentIdentifierSetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<InstrumentIdentifierUnsetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<InstrumentTermsSetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<InstrumentPriceSetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<InstrumentIncomeSetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<TransactionCreditEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<TransactionDebitEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<TransactionCancellationEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<ITicket>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<IUserEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<IUserMenuPreferencesEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<IUserValuationPreferencesEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<IUserBookmarksEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<IValuationSettingEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<AggregateUpdateNotificationService>());
        services.AddSingleton<AggregateCacheInvalidationService>();
        services.AddSingleton<AggregateCacheClearService>();

        return services;
    }
}
