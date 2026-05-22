using Marten;
using FolioTrace.Aggregates;
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
            options.Schema.For<FXDefinitionReadModel>().Index(model => model.Pair);
            options.Schema.For<FXDefinitionReadModel>().Index(model => model.ValidFrom);
            options.Schema.For<FXDefinitionReadModel>().Index(model => model.ValidTo);
            options.Schema.For<FXDefinitionReadModel>().Index(model => new { model.ValidFrom, model.ValidTo });
            options.Schema.For<FXRatePointReadModel>().Index(model => model.Pair);
            options.Schema.For<FXRatePointReadModel>().Index(model => model.ValidFrom);
            options.Schema.For<FXRatePointReadModel>().Index(model => model.ValidTo);
            options.Schema.For<FXRatePointReadModel>().Index(model => new { model.ValidFrom, model.ValidTo });
            options.Schema.For<InstrumentDefinitionReadModel>().Index(model => model.InstrumentID);
            options.Schema.For<InstrumentDefinitionReadModel>().Index(model => model.ValidFrom);
            options.Schema.For<InstrumentDefinitionReadModel>().Index(model => model.ValidTo);
            options.Schema.For<InstrumentPricePointReadModel>().Index(model => model.InstrumentID);
            options.Schema.For<InstrumentPricePointReadModel>().Index(model => model.ValidFrom);
            options.Schema.For<InstrumentPricePointReadModel>().Index(model => model.ValidTo);
            options.Schema.For<InstrumentIncomePointReadModel>().Index(model => model.InstrumentID);
            options.Schema.For<InstrumentIncomePointReadModel>().Index(model => model.ValidFrom);
            options.Schema.For<InstrumentIncomePointReadModel>().Index(model => model.ValidTo);
        });

        services.AddSingleton<MartenEventRepository>();
        services.AddScoped<IApiExchangeRepository, MartenApiExchangeRepository>();
        services.AddSingleton<IEventRepository, InMemoryEventsRepository>();
        services.AddSingleton<IFXRateReadModelRepository, MartenFXRateReadModelRepository>();
        services.AddSingleton<IInstrumentValueReadModelRepository, MartenInstrumentValueReadModelRepository>();
        services.AddHostedService<InMemoryEventsRepositoryInitializer>();
        services.AddScoped<ISeedRepository, SeedRepository>();

        return services;
    }

    private static IEnumerable<Type> GetEventTypes() =>
        typeof(IEventBase).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEventBase).IsAssignableFrom(type));
}

