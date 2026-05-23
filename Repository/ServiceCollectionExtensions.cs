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
        connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? CreateConnectionStringFromDatabaseUrl(configuration["DATABASE_URL"])
            : connectionString;

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

    private static string? CreateConnectionStringFromDatabaseUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
            return null;

        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme is not ("postgres" or "postgresql"))
            return null;

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
        var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        var port = uri.Port > 0 ? uri.Port : 5432;
        var connectionString = $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password}";

        if (databaseUrl.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase))
            connectionString += ";SSL Mode=Require;Trust Server Certificate=true";

        return connectionString;
    }
}

