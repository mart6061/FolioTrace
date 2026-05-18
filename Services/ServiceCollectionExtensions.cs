using Microsoft.Extensions.DependencyInjection;

namespace Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolioTraceServices(this IServiceCollection services)
    {
        services.AddSingleton<CountryService>();
        services.AddSingleton<CurrencyService>();

        return services;
    }
}
