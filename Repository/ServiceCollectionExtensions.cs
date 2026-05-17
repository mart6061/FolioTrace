using Marten;
using FolioTrace.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Repository;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolioTraceRepository(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var connectionString = configuration.GetConnectionString("FolioTrace");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'FolioTrace' was not found.");

        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.Events.AddEventTypes(GetEventTypes());
        });

        services.AddSingleton<MartenEventRepository>();
        services.AddSingleton<IEventRepository, InMemoryEventsRepository>();
        services.AddHostedService<InMemoryEventsRepositoryInitializer>();
        services.AddScoped<IInitRepository, InitRepository>();

        return services;
    }

    private static IEnumerable<Type> GetEventTypes() =>
        typeof(IEventBase).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEventBase).IsAssignableFrom(type));
}
