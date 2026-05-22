using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Services;

public static class DataValidationService
{
    public static async Task<IReadOnlyList<string>> ValidateCountries(CountryService countryService)
    {
        if (countryService is null)
            throw new ArgumentNullException(nameof(countryService));

        var countries = await countryService.Get(Constants.Valuation.Today, AuditDateTimeBuilder.CreateEndOfTime());
        return ValidateCountries(countries);
    }

    public static IReadOnlyList<string> ValidateCountries(Countries countries)
    {
        if (countries is null)
            throw new ArgumentNullException(nameof(countries));

        var issues = new List<string>();

        if (countries.Items.Count == 0)
            issues.Add("Countries must contain at least one item.");

        AddDuplicateIssues(
            issues,
            countries.Items,
            country => country.Alpha2.Value,
            "Country Alpha2");

        AddDuplicateIssues(
            issues,
            countries.Items,
            country => country.Alpha3.Value,
            "Country Alpha3");

        AddDuplicateIssues(
            issues,
            countries.Items,
            country => country.Numeric,
            "Country numeric code");

        foreach (var country in countries.Items)
        {
            if (string.IsNullOrWhiteSpace(country.Name))
                issues.Add($"Country '{country.Alpha2}' must have a name.");
        }

        return issues;
    }

    public static async Task<IReadOnlyList<string>> ValidateCurrencies(CurrencyService currencyService)
    {
        if (currencyService is null)
            throw new ArgumentNullException(nameof(currencyService));

        var currencies = await currencyService.Get(Constants.Valuation.Today, AuditDateTimeBuilder.CreateEndOfTime());
        return ValidateCurrencies(currencies);
    }

    public static IReadOnlyList<string> ValidateCurrencies(Currencies currencies)
    {
        if (currencies is null)
            throw new ArgumentNullException(nameof(currencies));

        var issues = new List<string>();

        if (currencies.Items.Count == 0)
            issues.Add("Currencies must contain at least one item.");

        AddDuplicateIssues(
            issues,
            currencies.Items,
            currency => currency.AlphabeticCode.Value,
            "Currency alphabetic code");

        AddDuplicateIssues(
            issues,
            currencies.Items,
            currency => currency.NumericCode,
            "Currency numeric code");

        foreach (var currency in currencies.Items)
        {
            if (currency.DecimalPlace < 0)
                issues.Add($"Currency '{currency.AlphabeticCode}' must have a non-negative decimal place.");

            if (string.IsNullOrWhiteSpace(currency.Name))
                issues.Add($"Currency '{currency.AlphabeticCode}' must have a name.");
        }

        return issues;
    }

    public static async Task<IReadOnlyList<string>> ValidateCurrencyCountryCoverage(CountryService countryService, CurrencyService currencyService)
    {
        if (countryService is null)
            throw new ArgumentNullException(nameof(countryService));

        if (currencyService is null)
            throw new ArgumentNullException(nameof(currencyService));

        var asAt = AuditDateTimeBuilder.CreateEndOfTime();
        var countries = await countryService.Get(Constants.Valuation.Today, asAt);
        var currencies = await currencyService.Get(Constants.Valuation.Today, asAt);

        return ValidateCurrencyCountryCoverage(countries, currencies);
    }

    public static IReadOnlyList<string> ValidateCurrencyCountryCoverage(Countries countries, Currencies currencies)
    {
        if (countries is null)
            throw new ArgumentNullException(nameof(countries));

        if (currencies is null)
            throw new ArgumentNullException(nameof(currencies));

        var countryAlpha3Codes = countries.Items
            .Select(country => country.Alpha3.Value)
            .ToHashSet(StringComparer.Ordinal);

        return currencies.Items
            .Where(currency => !countryAlpha3Codes.Contains(currency.AlphabeticCode.Value))
            .Select(currency => $"Currency '{currency.AlphabeticCode}' has no corresponding country Alpha3 entry.")
            .ToList();
    }

    public static async Task<IReadOnlyList<string>> ValidateReferenceData(CountryService countryService, CurrencyService currencyService)
    {
        if (countryService is null)
            throw new ArgumentNullException(nameof(countryService));

        if (currencyService is null)
            throw new ArgumentNullException(nameof(currencyService));

        var asAt = AuditDateTimeBuilder.CreateEndOfTime();
        var countries = await countryService.Get(Constants.Valuation.Today, asAt);
        var currencies = await currencyService.Get(Constants.Valuation.Today, asAt);

        return
        [
            .. ValidateCountries(countries),
            .. ValidateCurrencies(currencies),
            .. ValidateCurrencyCountryCoverage(countries, currencies)
        ];
    }

    private static void AddDuplicateIssues<TItem, TValue>(List<string> issues, IEnumerable<TItem> items, Func<TItem, TValue> selector, string label)
        where TValue : notnull
    {
        foreach (var duplicate in items.GroupBy(selector).Where(group => group.Count() > 1))
            issues.Add($"{label} '{duplicate.Key}' is duplicated.");
    }
}
