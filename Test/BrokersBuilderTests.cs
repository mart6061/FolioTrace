using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Test;

public sealed class BrokersBuilderTests
{
    [Fact]
    public void Create_ThrowsOnNullEventDate()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BrokersBuilder.Create(null!, AuditDateTimeBuilder.Create(), []));
    }

    [Fact]
    public void Create_ThrowsOnNullAuditDateTime()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BrokersBuilder.Create(EventDate, null!, []));
    }

    [Fact]
    public void Create_ThrowsOnNullEvents()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BrokersBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), null!));
    }

    [Fact]
    public void Create_ThrowsWhenEventsContainNull()
    {
        var events = new List<IBrokerEvent> { null! };

        var exception = Assert.Throws<ArgumentException>(() =>
            BrokersBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), events));

        Assert.Contains("must not contain null broker events", exception.Message);
    }

    [Fact]
    public void Create_AllowsEmptyEvents()
    {
        var brokers = BrokersBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), []);

        Assert.Empty(brokers.Items);
    }

    [Fact]
    public void Create_BuildsBrokersFromSeedEvents()
    {
        var events = SeedRepository.CreateInitialBrokerCreatedEvents().Cast<IBrokerEvent>().ToList();

        var brokers = BrokersBuilder.Create(new EventDateTime(DateTime.MaxValue), AuditDateTimeBuilder.Create(), events);

        Assert.Equal(events.Count, brokers.Items.Count);
    }

    [Fact]
    public void GetBrokerEventTypes_ReturnsSupportedEventTypes()
    {
        var types = BrokersBuilder.GetBrokerEventTypes();

        Assert.Contains(typeof(BrokerCreatedEvent), types);
        Assert.Contains(typeof(BrokerModifiedEvent), types);
        Assert.Contains(typeof(BrokerActiveSetEvent), types);
        Assert.Contains(typeof(BrokerApprovedDateTimeSetEvent), types);
        Assert.Contains(typeof(BrokerNextReviewSetEvent), types);
        Assert.Contains(typeof(BrokerNotesSetEvent), types);
    }

    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10));
}
