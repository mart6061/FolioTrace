using System.Reflection;
using FolioTrace.Aggregates;
using FolioTrace.Common;

namespace Test;

public sealed class FeatureAggregateMetadataTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedAggregateDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(Accounts)] = "Accounts",
            [nameof(Brokers)] = "Brokers",
            [nameof(Countries)] = "Countries",
            [nameof(Currencies)] = "Currencies",
            [nameof(FXs)] = "Foreign exchange pairs",
            [nameof(FXRates)] = "Foreign exchange rates",
            [nameof(Instruments)] = "Instruments",
            [nameof(InstrumentValues)] = "Instrument prices and income",
            [nameof(Holdings)] = "Holdings",
            [nameof(HoldingPositions)] = "Holding positions",
            [nameof(InputControlSettings)] = "Input control settings",
            [nameof(Valuations)] = "Portfolio valuations",
            [nameof(ProfitLosses)] = "Profit and loss",
            [nameof(ReportConfigs)] = "Report configurations",
            [nameof(Tickets)] = "Trade tickets",
            [nameof(TicketDetails)] = "Trade ticket details",
            [nameof(Users)] = "Users",
            [nameof(FoleoTraderOrders)] = "Foleo Trader orders",
            [nameof(ValuationSettings)] = "Valuation settings",
            [nameof(AssetAllocationMappings)] = "Asset allocation mappings"
        };

    [Fact]
    public void AggregateTypesHaveFeatureAggregateAttribute()
    {
        var missing = IncludedAggregateTypes()
            .Where(type => type.GetCustomAttribute<FeatureAggregateAttribute>(inherit: false) is null)
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void FeatureAggregateDescriptionsMatchExpectedUiLabels()
    {
        var mismatches = IncludedAggregateTypes()
            .Select(type => new
            {
                Type = type,
                Attribute = type.GetCustomAttribute<FeatureAggregateAttribute>(inherit: false)
            })
            .Where(item => item.Attribute?.Description != ExpectedAggregateDescriptions[item.Type.Name])
            .Select(item => $"{item.Type.Name}: expected {ExpectedAggregateDescriptions[item.Type.Name]}, actual {item.Attribute?.Description ?? "<missing>"}")
            .OrderBy(message => message)
            .ToList();

        Assert.Empty(mismatches);
    }

    [Fact]
    public void FeatureAggregateDescriptionsArePopulated()
    {
        var empty = IncludedAggregateTypes()
            .Select(type => new
            {
                Type = type,
                Attribute = type.GetCustomAttribute<FeatureAggregateAttribute>(inherit: false)
            })
            .Where(item => string.IsNullOrWhiteSpace(item.Attribute?.Description))
            .Select(item => item.Type.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.Empty(empty);
    }

    [Fact]
    public void PublicStaticFeatureBuildersHaveBuilderAttribute()
    {
        var missing = PublicStaticBuilderTypes()
            .Where(type => type.GetCustomAttribute<BuilderAttribute>(inherit: false) is null)
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.Empty(missing);
    }

    private static IReadOnlyList<Type> IncludedAggregateTypes()
    {
        var aggregateTypes = typeof(IAggregate).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true })
            .Where(type => typeof(IAggregate).IsAssignableFrom(type))
            .OrderBy(type => type.Name)
            .ToList();

        Assert.Equal(ExpectedAggregateDescriptions.Keys.OrderBy(name => name), aggregateTypes.Select(type => type.Name).OrderBy(name => name));
        return aggregateTypes;
    }

    private static IReadOnlyList<Type> PublicStaticBuilderTypes() =>
        typeof(IAggregate).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: true, IsSealed: true, IsPublic: true })
            .Where(type => type.Name.EndsWith("Builder", StringComparison.Ordinal))
            .OrderBy(type => type.FullName)
            .ToList();
}
