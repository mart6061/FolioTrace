using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class CurrenciesBuilderTests
{
    [Fact]
    public void Create_ThrowsOnNullEventDate()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CurrenciesBuilder.Create(null!, AuditDateTimeBuilder.Create(), [CreateCurrencyEvent("USD", 840, 2, "US Dollar")]));
    }

    [Fact]
    public void Create_ThrowsOnNullAuditDateTime()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CurrenciesBuilder.Create(EventDate, null!, [CreateCurrencyEvent("USD", 840, 2, "US Dollar")]));
    }

    [Fact]
    public void Create_ThrowsOnNullEvents()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CurrenciesBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), null!));
    }

    [Fact]
    public void Create_ThrowsWhenEventsContainNull()
    {
        var events = new List<ICurrencyEvent> { CreateCurrencyEvent("USD", 840, 2, "US Dollar"), null! };

        var exception = Assert.Throws<ArgumentException>(() =>
            CurrenciesBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), events));

        Assert.Contains("must not contain null currency events", exception.Message);
    }

    [Fact]
    public void Create_ThrowsWhenEventsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CurrenciesBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), []));

        Assert.Contains("must contain at least one currency event", exception.Message);
    }

    [Fact]
    public void Create_BuildsCurrenciesFromEvents()
    {
        var events = new List<ICurrencyEvent>
        {
            CreateCurrencyEvent("USD", 840, 2, "US Dollar"),
            CreateCurrencyEvent("EUR", 978, 2, "Euro")
        };

        var currencies = CurrenciesBuilder.Create(EventDate, AuditDateTimeBuilder.Create(), events);

        Assert.Equal(2, currencies.Items.Count);
        Assert.Contains(currencies.Items, currency => currency.AlphabeticCode == Alpha3Builder.Create("USD"));
        Assert.Contains(currencies.Items, currency => currency.AlphabeticCode == Alpha3Builder.Create("EUR"));
    }

    [Fact]
    public void GetCurrencyEventTypes_ReturnsSupportedEventTypes()
    {
        var types = CurrenciesBuilder.GetCurrencyEventTypes();

        Assert.Contains(typeof(CurrencyCreatedEvent), types);
        Assert.Contains(typeof(CurrencyModifiedEvent), types);
    }

    private static readonly UserID UserID = new(Guid.Parse("2aaf4fa2-3d22-4420-90ac-03a028cebbeb"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10));

    private static CurrencyCreatedEvent CreateCurrencyEvent(string alphabeticCode, int numericCode, short decimalPlace, string name) =>
        CurrencyCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-9)),
            "Create currency",
            Alpha3Builder.Create(alphabeticCode),
            numericCode,
            decimalPlace,
            name).Value!;
}
