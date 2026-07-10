using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Test;

public sealed class BrokerTradeMethodTests
{
    [Fact]
    public void SeedConfiguresManualForEveryBrokerAndEligibleExecutionMethods()
    {
        var created = SeedRepository.CreateInitialBrokerCreatedEvents();
        var methodEvents = SeedRepository.CreateInitialBrokerTradeMethodSetEvents();
        var events = created.Cast<IBrokerEvent>().Concat(methodEvents).ToList();
        var brokers = new Brokers(new EventDateTime(DateTime.MaxValue), AuditDateTimeBuilder.Create(), events);

        Assert.All(brokers.Items, broker => Assert.Single(broker.TradeMethods, method => method.Type == TradeMethodType.Manual));
        Assert.Single(brokers.Items.SelectMany(broker => broker.TradeMethods.OfType<FIXTradeMethod>()));
        Assert.Single(brokers.Items.SelectMany(broker => broker.TradeMethods.OfType<TradeFileTradeMethod>()));
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
