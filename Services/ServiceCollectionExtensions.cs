using FolioTrace.Aggregates;
using Microsoft.Extensions.DependencyInjection;

namespace Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolioTraceServices(this IServiceCollection services)
    {
        services.AddSingleton<CountryService>();
        services.AddSingleton<CurrencyService>();
        services.AddSingleton<IAggregateCacheInvalidator<CountryCreatedEvent>>(provider => new AggregateCacheInvalidator<CountryCreatedEvent>(provider.GetRequiredService<CountryService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CountryModifiedEvent>>(provider => new AggregateCacheInvalidator<CountryModifiedEvent>(provider.GetRequiredService<CountryService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CountryFlagModifiedEvent>>(provider => new AggregateCacheInvalidator<CountryFlagModifiedEvent>(provider.GetRequiredService<CountryService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CurrencyCreatedEvent>>(provider => new AggregateCacheInvalidator<CurrencyCreatedEvent>(provider.GetRequiredService<CurrencyService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator<CurrencyModifiedEvent>>(provider => new AggregateCacheInvalidator<CurrencyModifiedEvent>(provider.GetRequiredService<CurrencyService>().Invalidate));
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CountryFlagModifiedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CurrencyCreatedEvent>>());
        services.AddSingleton<IAggregateCacheInvalidator>(provider => provider.GetRequiredService<IAggregateCacheInvalidator<CurrencyModifiedEvent>>());
        services.AddSingleton<AggregateCacheInvalidationService>();

        return services;
    }
}
