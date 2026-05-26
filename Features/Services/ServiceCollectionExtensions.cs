using FolioTrace.Aggregates;
using Microsoft.Extensions.DependencyInjection;

namespace Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolioTraceServices(this IServiceCollection services)
    {
        services.AddSingleton<AccountService>();
        services.AddSingleton<CountryService>();
        services.AddSingleton<CurrencyService>();
        services.AddSingleton<FXService>();
        services.AddSingleton<FXRateService>();
        services.AddSingleton<HoldingService>();
        services.AddSingleton<HoldingPositionService>();
        services.AddSingleton<InstrumentService>();
        services.AddSingleton<InstrumentValueService>();
        services.AddSingleton<AggregateMaintenanceCoordinator>();
        services.AddSingleton<AggregateUpdateNotificationService>();
        services.AddSingleton<IAggregateCacheInvalidator<AccountCreatedEvent>>(provider => new AggregateCacheInvalidator<AccountCreatedEvent>(@event =>
            provider.GetRequiredService<AccountService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<AccountModifiedEvent>>(provider => new AggregateCacheInvalidator<AccountModifiedEvent>(@event =>
            provider.GetRequiredService<AccountService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<AccountActiveModifiedEvent>>(provider => new AggregateCacheInvalidator<AccountActiveModifiedEvent>(@event =>
            provider.GetRequiredService<AccountService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
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
        services.AddSingleton<IAggregateCacheInvalidator<HoldingCreatedEvent>>(provider => new AggregateCacheInvalidator<HoldingCreatedEvent>(@event =>
            provider.GetRequiredService<HoldingService>().Invalidate(@event) + provider.GetRequiredService<HoldingPositionService>().Invalidate(@event)));
        services.AddSingleton<IAggregateCacheInvalidator<HoldingModifiedEvent>>(provider => new AggregateCacheInvalidator<HoldingModifiedEvent>(@event =>
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
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<AccountCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<AccountModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<AccountActiveModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryFlagModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CurrencyCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CurrencyModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<FXCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<FXActiveModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<FXRateSetEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<HoldingCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<HoldingModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<HoldingActiveModifiedEvent>>());
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
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<AggregateUpdateNotificationService>());
        services.AddSingleton<AggregateCacheInvalidationService>();
        services.AddSingleton<AggregateCacheClearService>();

        return services;
    }
}
