using System.Text.Json;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Test;

public sealed class BrokerTradeMethodTests
{
    [Theory]
    [InlineData("FTP", typeof(FTPTradeMethodFileSendConfig))]
    [InlineData("Email", typeof(EmailTradeMethodFileSendConfig))]
    public void LegacyTradeMethodFileSendConfigJsonWithoutDiscriminatorDeserializes(string type, Type expectedType)
    {
        var configuration = type switch
        {
            "FTP" => (ITradeMethodFileSendConfig)new FTPTradeMethodFileSendConfig("localhost", 21, "/incoming", "user", null),
            "Email" => new EmailTradeMethodFileSendConfig(new EmailAddress("trades@example.com")),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        var legacyJson = JsonSerializer.Serialize(configuration, configuration.GetType());

        var restored = JsonSerializer.Deserialize<ITradeMethodFileSendConfig>(legacyJson);

        Assert.IsType(expectedType, restored);
    }

    [Fact]
    public void TradeMethodFileSendConfigJsonWritesDiscriminatorAndRoundTrips()
    {
        ITradeMethodFileSendConfig configuration = new FTPTradeMethodFileSendConfig("localhost", 21, "/incoming", "user", null);

        var json = JsonSerializer.Serialize(configuration);
        var restored = JsonSerializer.Deserialize<ITradeMethodFileSendConfig>(json);

        Assert.Contains("\"$type\":\"FTPTradeMethodFileSendConfig\"", json);
        Assert.IsType<FTPTradeMethodFileSendConfig>(restored);
    }

    [Theory]
    [InlineData("Manual", typeof(ManualTradeMethod))]
    [InlineData("FIX", typeof(FIXTradeMethod))]
    [InlineData("Phone", typeof(PhoneTradeMethod))]
    [InlineData("Fax", typeof(FaxTradeMethod))]
    [InlineData("TradeFile", typeof(TradeFileTradeMethod))]
    public void LegacyTradeMethodJsonWithoutDiscriminatorDeserializes(string type, Type expectedType)
    {
        var method = type switch
        {
            "Manual" => (ITradeMethod)new ManualTradeMethod(),
            "FIX" => new FIXTradeMethod("localhost", 1234, "SENDER", "TARGET"),
            "Phone" => new PhoneTradeMethod(new TelephoneNumber("+44 20 1234 5678")),
            "Fax" => new FaxTradeMethod(new TelephoneNumber("+44 20 8765 4321")),
            "TradeFile" => SeedRepository.CreateInitialBrokerTradeMethodSetEvents()
                .OfType<BrokerTradeMethodSetEventBase>()
                .Select(@event => @event.TradeMethod)
                .First(method => method.Type == TradeMethodType.TradeFile),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        var legacyJson = JsonSerializer.Serialize(method, method.GetType());

        var restored = JsonSerializer.Deserialize<ITradeMethod>(legacyJson);

        Assert.IsType(expectedType, restored);
    }

    [Fact]
    public void TradeMethodJsonWritesDiscriminatorAndRoundTrips()
    {
        ITradeMethod method = new ManualTradeMethod();

        var json = JsonSerializer.Serialize(method);
        var restored = JsonSerializer.Deserialize<ITradeMethod>(json);

        Assert.Contains("\"$type\":\"ManualTradeMethod\"", json);
        Assert.IsType<ManualTradeMethod>(restored);
    }
    [Fact]
    public void SeedConfiguresManualForEveryBrokerAndEligibleExecutionMethods()
    {
        var created = SeedRepository.CreateInitialBrokerCreatedEvents();
        var methodEvents = SeedRepository.CreateInitialBrokerTradeMethodSetEvents();
        var events = created.Cast<IBrokerEvent>().Concat(methodEvents).ToList();
        var brokers = new Brokers(new EventDateTime(DateTime.MaxValue), AuditDateTimeBuilder.Create(), events);

        Assert.All(brokers.Items, broker => Assert.Single(broker.TradeMethods, method => method.Type == TradeMethodType.Manual));
        Assert.Single(brokers.Items.SelectMany(broker => broker.TradeMethods.OfType<FIXTradeMethod>()));
        var tradeFileMethods = brokers.Items.SelectMany(broker => broker.TradeMethods.OfType<TradeFileTradeMethod>()).ToList();
        Assert.Equal(3, tradeFileMethods.Count);
        Assert.Single(tradeFileMethods.Select(method => method.SendConfig).Distinct());
        Assert.All(brokers.Items, broker => Assert.Equal(broker.TradeMethods.Count, broker.TradeMethods.Select(method => method.Type).Distinct().Count()));
    }

    [Fact]
    public void SetReplacesMethodOfSameTypeAndUnsetRemovesIt()
    {
        var created = SeedRepository.CreateInitialBrokerCreatedEvents()[0];
        var first = BrokerTradeMethodEventBuilder.Set(new(
            created.UserID, created.EventDateTime, "Set FIX", created.LEI,
            new FIXTradeMethod("first", 1001, "S", "T"))).Value!;
        var second = BrokerTradeMethodEventBuilder.Set(new(
            created.UserID, new EventDateTime(created.EventDateTime.Value.AddSeconds(1)), "Replace FIX", created.LEI,
            new FIXTradeMethod("second", 1002, "S2", "T2"))).Value!;
        var unset = BrokerTradeMethodEventBuilder.Unset(new(
            created.UserID, new EventDateTime(created.EventDateTime.Value.AddSeconds(2)), "Unset FIX", created.LEI,
            TradeMethodType.FIX)).Value!;
        var brokers = new Brokers(new EventDateTime(DateTime.MaxValue), AuditDateTimeBuilder.Create(), [created, first, second, unset]);

        Assert.Empty(brokers.Items.Single().TradeMethods);
    }
}
