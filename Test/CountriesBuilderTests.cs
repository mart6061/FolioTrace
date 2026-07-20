using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class CountriesBuilderTests
{
    [Fact]
    public void Create_ThrowsOnNullEventDate()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CountriesBuilder.Create(null!, AuditDateTimeBuilder.Create(), [CreateCountryEvent("US", "USA", 840, "United States")]));
    }

    [Fact]
    public void Create_ThrowsOnNullAuditDateTime()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CountriesBuilder.Create(EventDate, null!, [CreateCountryEvent("US", "USA", 840, "United States")]));
    }

    [Fact]
    public void Create_ThrowsOnNullEvents()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CountriesBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), null!));
    }

    [Fact]
    public void Create_ThrowsWhenEventsContainNull()
    {
        var events = new List<ICountryEvent> { CreateCountryEvent("US", "USA", 840, "United States"), null! };

        var exception = Assert.Throws<ArgumentException>(() =>
            CountriesBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), events));

        Assert.Contains("must not contain null country events", exception.Message);
    }

    [Fact]
    public void Create_ThrowsWhenEventsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CountriesBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), []));

        Assert.Contains("must contain at least one country event", exception.Message);
    }

    [Fact]
    public void Create_BuildsCountriesFromEvents()
    {
        var events = new List<ICountryEvent>
        {
            CreateCountryEvent("US", "USA", 840, "United States"),
            CreateCountryEvent("CA", "CAN", 124, "Canada")
        };

        var countries = CountriesBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), events);

        Assert.Equal(2, countries.Items.Count);
        Assert.Contains(countries.Items, country => country.Alpha2 == Alpha2Builder.Create("US"));
        Assert.Contains(countries.Items, country => country.Alpha2 == Alpha2Builder.Create("CA"));
    }

    [Fact]
    public void GetCountryEventTypes_ReturnsSupportedEventTypes()
    {
        var types = CountriesBuilder.GetCountryEventTypes();

        Assert.Contains(typeof(CountryCreatedEvent), types);
        Assert.Contains(typeof(CountryModifiedEvent), types);
        Assert.Contains(typeof(CountryFlagModifiedEvent), types);
    }

    private static readonly UserID UserID = new(Guid.Parse("2aaf4fa2-3d22-4420-90ac-03a028cebbeb"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10));

    private static CountryCreatedEvent CreateCountryEvent(string alpha2, string alpha3, short numeric, string name) =>
        CountryCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-9)),
            "Create country",
            Alpha2Builder.Create(alpha2),
            Alpha3Builder.Create(alpha3),
            numeric,
            name).Value!;
}
