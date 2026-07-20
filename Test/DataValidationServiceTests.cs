using FolioTrace.Aggregates;
using FolioTrace.Types;
using Services;

namespace Test;

public sealed class DataValidationServiceTests
{
    [Fact]
    public void ValidateCountries_ReturnsNoIssues_ForValidCountries()
    {
        var countries = CreateCountries(
            ("US", "USA", 840, "United States"),
            ("CA", "CAN", 124, "Canada"));

        var issues = DataValidationService.ValidateCountries(countries);

        Assert.Empty(issues);
    }

    [Fact]
    public void ValidateCountries_FlagsEmptyCollection()
    {
        var issues = DataValidationService.ValidateCountries(CreateEmptyCountries());

        Assert.Contains("Countries must contain at least one item.", issues);
    }

    [Fact]
    public void ValidateCountries_FlagsDuplicateAlpha3()
    {
        var countries = CreateCountries(
            ("US", "USA", 840, "United States"),
            ("CA", "USA", 124, "Canada"));

        var issues = DataValidationService.ValidateCountries(countries);

        Assert.Contains("Country Alpha3 'USA' is duplicated.", issues);
    }

    [Fact]
    public void ValidateCountries_FlagsDuplicateNumericCode()
    {
        var countries = CreateCountries(
            ("US", "USA", 840, "United States"),
            ("CA", "CAN", 840, "Canada"));

        var issues = DataValidationService.ValidateCountries(countries);

        Assert.Contains("Country numeric code '840' is duplicated.", issues);
    }

    [Fact]
    public void ValidateCountries_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => DataValidationService.ValidateCountries((Countries)null!));
    }

    [Fact]
    public void ValidateCurrencies_ReturnsNoIssues_ForValidCurrencies()
    {
        var currencies = CreateCurrencies(
            ("USD", 840, 2, "US Dollar"),
            ("EUR", 978, 2, "Euro"));

        var issues = DataValidationService.ValidateCurrencies(currencies);

        Assert.Empty(issues);
    }

    [Fact]
    public void ValidateCurrencies_FlagsEmptyCollection()
    {
        var issues = DataValidationService.ValidateCurrencies(CreateEmptyCurrencies());

        Assert.Contains("Currencies must contain at least one item.", issues);
    }

    [Fact]
    public void ValidateCurrencies_FlagsDuplicateNumericCode()
    {
        var currencies = CreateCurrencies(
            ("USD", 840, 2, "US Dollar"),
            ("EUR", 840, 2, "Euro"));

        var issues = DataValidationService.ValidateCurrencies(currencies);

        Assert.Contains("Currency numeric code '840' is duplicated.", issues);
    }

    [Fact]
    public void ValidateCurrencies_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => DataValidationService.ValidateCurrencies((Currencies)null!));
    }

    [Fact]
    public void ValidateCurrencyCountryCoverage_ReturnsNoIssues_WhenEveryCurrencyHasCountry()
    {
        var countries = CreateCountries(("US", "USD", 840, "United States"));
        var currencies = CreateCurrencies(("USD", 840, 2, "US Dollar"));

        var issues = DataValidationService.ValidateCurrencyCountryCoverage(countries, currencies);

        Assert.Empty(issues);
    }

    [Fact]
    public void ValidateCurrencyCountryCoverage_FlagsCurrencyWithoutMatchingCountry()
    {
        var countries = CreateCountries(("US", "USA", 840, "United States"));
        var currencies = CreateCurrencies(("USD", 840, 2, "US Dollar"));

        var issues = DataValidationService.ValidateCurrencyCountryCoverage(countries, currencies);

        Assert.Contains("Currency 'USD' has no corresponding country Alpha3 entry.", issues);
    }

    [Fact]
    public void ValidateCurrencyCountryCoverage_ThrowsOnNullCountries()
    {
        var currencies = CreateCurrencies(("USD", 840, 2, "US Dollar"));

        Assert.Throws<ArgumentNullException>(() => DataValidationService.ValidateCurrencyCountryCoverage(null!, currencies));
    }

    [Fact]
    public void ValidateCurrencyCountryCoverage_ThrowsOnNullCurrencies()
    {
        var countries = CreateCountries(("US", "USA", 840, "United States"));

        Assert.Throws<ArgumentNullException>(() => DataValidationService.ValidateCurrencyCountryCoverage(countries, null!));
    }

    [Fact]
    public async Task ValidateCountriesAsync_ThrowsOnNullService()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => DataValidationService.ValidateCountries((CountryService)null!));
    }

    [Fact]
    public async Task ValidateCurrenciesAsync_ThrowsOnNullService()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => DataValidationService.ValidateCurrencies((CurrencyService)null!));
    }

    [Fact]
    public async Task ValidateCurrencyCountryCoverageAsync_ThrowsOnNullServices()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => DataValidationService.ValidateCurrencyCountryCoverage((CountryService)null!, (CurrencyService)null!));
    }

    [Fact]
    public async Task ValidateReferenceDataAsync_ThrowsOnNullServices()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => DataValidationService.ValidateReferenceData(null!, null!));
    }

    private static readonly UserID UserID = new(Guid.Parse("2aaf4fa2-3d22-4420-90ac-03a028cebbeb"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-9));

    private static Countries CreateCountries(params (string Alpha2, string Alpha3, short Numeric, string Name)[] items)
    {
        var events = items.Select((item, index) => CountryCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(index)),
            "Create country",
            Alpha2Builder.Create(item.Alpha2),
            Alpha3Builder.Create(item.Alpha3),
            item.Numeric,
            item.Name).Value!).Cast<ICountryEvent>().ToList();

        return new Countries(EventDateTimeBuilder.Create(DateTime.UtcNow), AuditDateTimeBuilder.Create(), events);
    }

    private static Currencies CreateCurrencies(params (string AlphabeticCode, int NumericCode, short DecimalPlace, string Name)[] items)
    {
        var events = items.Select((item, index) => CurrencyCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(index)),
            "Create currency",
            Alpha3Builder.Create(item.AlphabeticCode),
            item.NumericCode,
            item.DecimalPlace,
            item.Name).Value!).Cast<ICurrencyEvent>().ToList();

        return new Currencies(EventDateTimeBuilder.Create(DateTime.UtcNow), AuditDateTimeBuilder.Create(), events);
    }

    private static Countries CreateEmptyCountries()
    {
        var futureEvent = CountryCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDateTimeBuilder.Create(DateTime.UtcNow.AddDays(1)),
            AuditDateTimeBuilder.Create(),
            "Create country",
            Alpha2Builder.Create("US"),
            Alpha3Builder.Create("USA"),
            840,
            "United States").Value!;

        return new Countries(EventDateTimeBuilder.Create(DateTime.UtcNow), AuditDateTimeBuilder.Create(), [futureEvent]);
    }

    private static Currencies CreateEmptyCurrencies()
    {
        var futureEvent = CurrencyCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDateTimeBuilder.Create(DateTime.UtcNow.AddDays(1)),
            AuditDateTimeBuilder.Create(),
            "Create currency",
            Alpha3Builder.Create("USD"),
            840,
            2,
            "US Dollar").Value!;

        return new Currencies(EventDateTimeBuilder.Create(DateTime.UtcNow), AuditDateTimeBuilder.Create(), [futureEvent]);
    }
}
